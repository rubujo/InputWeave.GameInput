using System.Globalization;
using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

internal static class GameInputRuntimeLoader
{
    internal const string LoaderPolicy = "MicrosoftCxxLoaderParity";

    private const string GameInputDllName = "GameInput.dll";
    private const string GameInputRedistDllName = "GameInputRedist.dll";
    private const string GameInputInitializeEntryPoint = "GameInputInitialize";
    private const string RedistRegistrySubKey = @"SOFTWARE\Microsoft\GameInput";
    private const string RedistRegistryValueName = "RedistDir";

    private const uint LoadLibrarySearchDllLoadDir = 0x00000100;
    private const uint LoadLibrarySearchSystem32 = 0x00000800;
    private const uint RrfRtRegSz = 0x00000002;
    private const uint RrfSubKeyWow6432Key = 0x00020000;
    private const int ErrorSuccess = 0;
    private const int ErrorFileNotFound = 2;
    private const int ErrorPathNotFound = 3;
    private const int ErrorProcNotFound = 127;
    private const int SucceededHResult = 0;
    private const int FailedHResult = unchecked((int)0x80004005);
    private const int FileNotFoundHResult = unchecked((int)0x80070002);
    private const int ProcNotFoundHResult = unchecked((int)0x8007007F);
    private const uint FixedFileInfoSignature = 0xFEEF04BD;

#if NET10_0_OR_GREATER
    private static readonly System.Threading.Lock s_syncRoot = new();
#else
    private static readonly object s_syncRoot = new();
#endif
    private static LoadedRuntime? s_loadedRuntime;

    internal static GameInputRuntimeInfo GetInfo()
    {
        return EnsureLoaded().Info;
    }

    internal static bool TryProbe(out GameInputRuntimeProbeInfo info)
    {
        info = Probe(keepLoaded: false, out _);
        return info.IsAvailable;
    }

    internal static int GameInputInitialize(ref Guid riid, out IntPtr ppv)
    {
        return EnsureLoaded().Initialize(ref riid, out ppv);
    }

    internal static GameInputRuntimeCandidate? SelectPreferredCandidate(IReadOnlyList<GameInputRuntimeCandidate> candidates)
    {
        // 對齊 Microsoft C++ loader：System32 redist 優先於 registry fallback，
        // 且 redist 版本大於或等於 inbox runtime 時選 redist。
        bool hasInbox = TryFindExistingCandidate(candidates, GameInputRuntimeModuleKind.SystemGameInput, out GameInputRuntimeCandidate inbox);
        bool hasSystemRedist = TryFindExistingCandidate(candidates, GameInputRuntimeModuleKind.SystemGameInputRedist, out GameInputRuntimeCandidate systemRedist);
        bool hasRegistryRedist = TryFindExistingCandidate(candidates, GameInputRuntimeModuleKind.RegistryGameInputRedist, out GameInputRuntimeCandidate registryRedist);
        bool hasRedist = hasSystemRedist || hasRegistryRedist;
        GameInputRuntimeCandidate redist = hasSystemRedist ? systemRedist : registryRedist;

        if (!hasInbox && !hasRedist)
        {
            return null;
        }

        if (!hasInbox)
        {
            return redist;
        }

        if (!hasRedist)
        {
            return inbox;
        }

        return CompareCandidateVersions(redist, inbox) >= 0
            ? redist
            : inbox;
    }

    internal static GameInputRuntimeCandidate CreateTestCandidate(GameInputRuntimeModuleKind moduleKind, bool exists, Version? fileVersion)
    {
        return new GameInputRuntimeCandidate(moduleKind, moduleKind.ToString(), exists, fileVersion, SucceededHResult, exists ? SucceededHResult : FileNotFoundHResult, LoadLibrarySearchSystem32);
    }

    private static LoadedRuntime EnsureLoaded()
    {
        LoadedRuntime? loaded = s_loadedRuntime;
        if (loaded is not null)
        {
            return loaded;
        }

        lock (s_syncRoot)
        {
            loaded = s_loadedRuntime;
            if (loaded is not null)
            {
                return loaded;
            }

            GameInputRuntimeProbeInfo probeInfo = Probe(keepLoaded: true, out loaded);
            if (loaded is null)
            {
                ThrowProbeFailure(probeInfo);
                throw new InvalidOperationException("GameInput runtime loader 未回傳載入結果。");
            }

            s_loadedRuntime = loaded;
            return loaded;
        }
    }

    private static GameInputRuntimeProbeInfo Probe(bool keepLoaded, out LoadedRuntime? loaded)
    {
        loaded = null;

        List<GameInputRuntimeCandidate> candidates = DiscoverCandidates();
        GameInputRuntimeCandidate? selected = SelectPreferredCandidate(candidates);
        if (selected is null)
        {
            return CreateProbeInfo(false, null, FileNotFoundHResult, (int)ErrorFileNotFound, candidates);
        }

        bool available = TryLoadSelectedCandidate(selected.Value, keepLoaded, out loaded, out GameInputRuntimeCandidateInfo selectedInfo);
        ReplaceCandidateInfo(candidates, selected.Value, selectedInfo);
        int finalHResult = selectedInfo.LoadHResult != SucceededHResult
            ? selectedInfo.LoadHResult
            : selectedInfo.GetProcAddressHResult;
        return CreateProbeInfo(available, selectedInfo, finalHResult, selectedInfo.Win32Error, candidates);
    }

    private static List<GameInputRuntimeCandidate> DiscoverCandidates()
    {
        List<GameInputRuntimeCandidate> candidates = [];

        int systemDirectoryHResult = TryGetSystemDirectory(out string systemDirectory);
        if (systemDirectoryHResult == SucceededHResult)
        {
            candidates.Add(CreateFileCandidate(GameInputRuntimeModuleKind.SystemGameInput, Path.Combine(systemDirectory, GameInputDllName), LoadLibrarySearchSystem32, SucceededHResult));
            candidates.Add(CreateFileCandidate(GameInputRuntimeModuleKind.SystemGameInputRedist, Path.Combine(systemDirectory, GameInputRedistDllName), LoadLibrarySearchSystem32, SucceededHResult));
        }
        else
        {
            candidates.Add(CreateFileCandidate(GameInputRuntimeModuleKind.SystemGameInput, string.Empty, LoadLibrarySearchSystem32, systemDirectoryHResult));
            candidates.Add(CreateFileCandidate(GameInputRuntimeModuleKind.SystemGameInputRedist, string.Empty, LoadLibrarySearchSystem32, systemDirectoryHResult));
        }

        int redistDirectoryHResult = TryGetRegistryRedistDirectory(out string redistDirectory);
        string registryRedistPath = string.IsNullOrWhiteSpace(redistDirectory)
            ? string.Empty
            : Path.Combine(redistDirectory, GameInputRedistDllName);
        // Registry redist 只允許 DLL 所在目錄與 System32 解析相依性，不回落到 app dir、cwd 或 PATH。
        candidates.Add(CreateFileCandidate(GameInputRuntimeModuleKind.RegistryGameInputRedist, registryRedistPath, LoadLibrarySearchDllLoadDir | LoadLibrarySearchSystem32, redistDirectoryHResult));

        return candidates;
    }

    private static GameInputRuntimeCandidate CreateFileCandidate(GameInputRuntimeModuleKind moduleKind, string modulePath, uint loadFlags, int discoveryHResult)
    {
        if (discoveryHResult != SucceededHResult || string.IsNullOrWhiteSpace(modulePath))
        {
            return new GameInputRuntimeCandidate(moduleKind, modulePath, exists: false, fileVersion: null, discoveryHResult, discoveryHResult, loadFlags);
        }

        bool exists = File.Exists(modulePath);
        if (!exists)
        {
            return new GameInputRuntimeCandidate(moduleKind, modulePath, exists: false, fileVersion: null, discoveryHResult, FileNotFoundHResult, loadFlags);
        }

        int fileVersionHResult = TryGetFileVersion(modulePath, out Version? fileVersion);
        return new GameInputRuntimeCandidate(moduleKind, modulePath, exists: true, fileVersion, discoveryHResult, fileVersionHResult, loadFlags);
    }

    private static bool TryFindExistingCandidate(IReadOnlyList<GameInputRuntimeCandidate> candidates, GameInputRuntimeModuleKind moduleKind, out GameInputRuntimeCandidate candidate)
    {
        foreach (GameInputRuntimeCandidate item in candidates)
        {
            if (item.ModuleKind == moduleKind && item.Exists)
            {
                candidate = item;
                return true;
            }
        }

        candidate = default;
        return false;
    }

    private static int CompareCandidateVersions(GameInputRuntimeCandidate left, GameInputRuntimeCandidate right)
    {
        Version leftVersion = left.FileVersion ?? new Version(0, 0, 0, 0);
        Version rightVersion = right.FileVersion ?? new Version(0, 0, 0, 0);
        return leftVersion.CompareTo(rightVersion);
    }

    private static bool TryLoadSelectedCandidate(GameInputRuntimeCandidate candidate, bool keepLoaded, out LoadedRuntime? loaded, out GameInputRuntimeCandidateInfo candidateInfo)
    {
        loaded = null;

        IntPtr module = Win32NativeMethods.LoadLibraryEx(candidate.ModulePath, IntPtr.Zero, candidate.LoadFlags);
        if (module == IntPtr.Zero)
        {
            int win32Error = Marshal.GetLastWin32Error();
            int hResult = HResultFromWin32(win32Error);
            candidateInfo = candidate.ToInfo(loadHResult: hResult, getProcAddressHResult: SucceededHResult, win32Error);
            return false;
        }

        try
        {
            IntPtr procName = Marshal.StringToHGlobalAnsi(GameInputInitializeEntryPoint);
            try
            {
                IntPtr entryPoint = Win32NativeMethods.GetProcAddress(module, procName);
                if (entryPoint == IntPtr.Zero)
                {
                    int win32Error = Marshal.GetLastWin32Error();
                    int hResult = win32Error == 0 ? ProcNotFoundHResult : HResultFromWin32(win32Error);
                    candidateInfo = candidate.ToInfo(loadHResult: SucceededHResult, getProcAddressHResult: hResult, win32Error);
                    return false;
                }

                string loadedPath = GetModuleFileName(module, candidate.ModulePath);
                GameInputInitializeDelegate initialize = Marshal.GetDelegateForFunctionPointer<GameInputInitializeDelegate>(entryPoint);
                GameInputRuntimeInfo info = new(LoaderPolicy, candidate.ModuleKind, loadedPath, candidate.FileVersion);
                candidateInfo = candidate.ToInfo(loadedPath, loadHResult: SucceededHResult, getProcAddressHResult: SucceededHResult, win32Error: 0);

                if (keepLoaded)
                {
                    // 保留 module handle 到程序結束，避免 GameInput COM 物件仍存活時 runtime 被卸載。
                    loaded = new LoadedRuntime(module, initialize, info);
                    module = IntPtr.Zero;
                }

                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(procName);
            }
        }
        finally
        {
            if (module != IntPtr.Zero)
            {
                _ = Win32NativeMethods.FreeLibrary(module);
            }
        }
    }

    private static GameInputRuntimeProbeInfo CreateProbeInfo(bool available, GameInputRuntimeCandidateInfo? selectedInfo, int hResult, int win32Error, IReadOnlyList<GameInputRuntimeCandidate> candidates)
    {
        GameInputRuntimeModuleKind moduleKind = available && selectedInfo.HasValue
            ? selectedInfo.Value.ModuleKind
            : GameInputRuntimeModuleKind.Unavailable;
        string modulePath = available && selectedInfo.HasValue
            ? selectedInfo.Value.ModulePath
            : string.Empty;
        Version? fileVersion = available && selectedInfo.HasValue
            ? selectedInfo.Value.FileVersion
            : null;

        GameInputRuntimeCandidateInfo[] candidateInfos = [.. candidates.Select(static item => item.ToInfo())];
        return new GameInputRuntimeProbeInfo(LoaderPolicy, available, moduleKind, modulePath, fileVersion, hResult, win32Error, candidateInfos);
    }

    private static void ReplaceCandidateInfo(List<GameInputRuntimeCandidate> candidates, GameInputRuntimeCandidate selected, GameInputRuntimeCandidateInfo selectedInfo)
    {
        for (int index = 0; index < candidates.Count; index++)
        {
            if (candidates[index].ModuleKind == selected.ModuleKind)
            {
                candidates[index] = candidates[index].WithProbeResult(selectedInfo);
                return;
            }
        }
    }

    private static void ThrowProbeFailure(GameInputRuntimeProbeInfo probeInfo)
    {
        string message = string.Format(
            CultureInfo.InvariantCulture,
            "找不到可用的 GameInput runtime。HRESULT: 0x{0:X8}; Win32: {1}; Policy: {2}。",
            probeInfo.HResult,
            probeInfo.Win32Error,
            probeInfo.LoaderPolicy);

        if (probeInfo.HResult == ProcNotFoundHResult)
        {
            throw new EntryPointNotFoundException(message);
        }

        throw new DllNotFoundException(message);
    }

    private static int TryGetSystemDirectory(out string systemDirectory)
    {
        systemDirectory = string.Empty;

        uint requiredLength = Win32NativeMethods.GetSystemDirectory(IntPtr.Zero, 0);
        if (requiredLength == 0)
        {
            return HResultFromLastWin32Error();
        }

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)(requiredLength + 1) * sizeof(char)));
        try
        {
            uint written = Win32NativeMethods.GetSystemDirectory(buffer, requiredLength + 1);
            if (written == 0)
            {
                return HResultFromLastWin32Error();
            }

            systemDirectory = Marshal.PtrToStringUni(buffer) ?? string.Empty;
            return string.IsNullOrWhiteSpace(systemDirectory)
                ? HResultFromWin32((int)ErrorPathNotFound)
                : SucceededHResult;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int TryGetRegistryRedistDirectory(out string redistDirectory)
    {
        redistDirectory = string.Empty;
        uint bufferSize = 0;
        int status = Win32NativeMethods.RegGetValue(Win32NativeMethods.s_hkeyLocalMachine, RedistRegistrySubKey, RedistRegistryValueName, RrfRtRegSz | RrfSubKeyWow6432Key, IntPtr.Zero, IntPtr.Zero, ref bufferSize);
        if (status != ErrorSuccess)
        {
            return HResultFromWin32(status);
        }

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)bufferSize));
        try
        {
            status = Win32NativeMethods.RegGetValue(Win32NativeMethods.s_hkeyLocalMachine, RedistRegistrySubKey, RedistRegistryValueName, RrfRtRegSz | RrfSubKeyWow6432Key, IntPtr.Zero, buffer, ref bufferSize);
            if (status != ErrorSuccess)
            {
                return HResultFromWin32(status);
            }

            redistDirectory = Marshal.PtrToStringUni(buffer) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(redistDirectory))
            {
                return HResultFromWin32((int)ErrorPathNotFound);
            }

            return SucceededHResult;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int TryGetFileVersion(string modulePath, out Version? fileVersion)
    {
        fileVersion = null;
        uint size = Win32NativeMethods.GetFileVersionInfoSize(modulePath, out _);
        if (size == 0)
        {
            return HResultFromLastWin32Error();
        }

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)size));
        try
        {
            if (!Win32NativeMethods.GetFileVersionInfo(modulePath, 0, size, buffer))
            {
                return HResultFromLastWin32Error();
            }

            IntPtr root = Marshal.StringToHGlobalUni("\\");
            try
            {
                if (!Win32NativeMethods.VerQueryValue(buffer, root, out IntPtr versionPointer, out uint versionLength) || versionLength < Marshal.SizeOf<VsFixedFileInfo>())
                {
                    return HResultFromLastWin32Error();
                }

                VsFixedFileInfo fixedFileInfo = Marshal.PtrToStructure<VsFixedFileInfo>(versionPointer);
                if (fixedFileInfo.Signature != FixedFileInfoSignature)
                {
                    return FailedHResult;
                }

                fileVersion = new Version(
                    HighWord(fixedFileInfo.FileVersionMS),
                    LowWord(fixedFileInfo.FileVersionMS),
                    HighWord(fixedFileInfo.FileVersionLS),
                    LowWord(fixedFileInfo.FileVersionLS));
                return SucceededHResult;
            }
            finally
            {
                Marshal.FreeHGlobal(root);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string GetModuleFileName(IntPtr module, string fallback)
    {
        const int MaxPath = 32767;
        IntPtr buffer = Marshal.AllocHGlobal(MaxPath * sizeof(char));
        try
        {
            uint length = Win32NativeMethods.GetModuleFileName(module, buffer, MaxPath);
            return length == 0
                ? fallback
                : Marshal.PtrToStringUni(buffer, checked((int)length)) ?? fallback;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int HResultFromLastWin32Error()
    {
        return HResultFromWin32(Marshal.GetLastWin32Error());
    }

    private static int HResultFromWin32(int error)
    {
        return error <= 0
            ? error
            : unchecked((int)(0x80070000 | (uint)error));
    }

    private static int HighWord(uint value)
    {
        return unchecked((int)((value >> 16) & 0xFFFF));
    }

    private static int LowWord(uint value)
    {
        return unchecked((int)(value & 0xFFFF));
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int GameInputInitializeDelegate(ref Guid riid, out IntPtr ppv);

    private sealed class LoadedRuntime
    {
        internal LoadedRuntime(IntPtr module, GameInputInitializeDelegate initialize, GameInputRuntimeInfo info)
        {
            Module = module;
            Initialize = initialize;
            Info = info;
        }

        internal IntPtr Module { get; }

        internal GameInputInitializeDelegate Initialize { get; }

        internal GameInputRuntimeInfo Info { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VsFixedFileInfo
    {
        public uint Signature;
        public uint StructVersion;
        public uint FileVersionMS;
        public uint FileVersionLS;
        public uint ProductVersionMS;
        public uint ProductVersionLS;
        public uint FileFlagsMask;
        public uint FileFlags;
        public uint FileOs;
        public uint FileType;
        public uint FileSubtype;
        public uint FileDateMS;
        public uint FileDateLS;
    }
}
