namespace InputWeave.GameInput;

internal readonly struct GameInputRuntimeCandidate
{
    public GameInputRuntimeCandidate(
        GameInputRuntimeModuleKind moduleKind,
        string modulePath,
        bool exists,
        Version? fileVersion,
        int discoveryHResult,
        int fileVersionHResult,
        uint loadFlags)
        : this(moduleKind, modulePath, exists, fileVersion, discoveryHResult, fileVersionHResult, loadFlags, 0, 0, 0)
    {
    }

    private GameInputRuntimeCandidate(
        GameInputRuntimeModuleKind moduleKind,
        string modulePath,
        bool exists,
        Version? fileVersion,
        int discoveryHResult,
        int fileVersionHResult,
        uint loadFlags,
        int loadHResult,
        int getProcAddressHResult,
        int win32Error)
    {
        ModuleKind = moduleKind;
        ModulePath = modulePath;
        Exists = exists;
        FileVersion = fileVersion;
        DiscoveryHResult = discoveryHResult;
        FileVersionHResult = fileVersionHResult;
        LoadFlags = loadFlags;
        LoadHResult = loadHResult;
        GetProcAddressHResult = getProcAddressHResult;
        Win32Error = win32Error;
    }

    public GameInputRuntimeModuleKind ModuleKind { get; }

    public string ModulePath { get; }

    public bool Exists { get; }

    public Version? FileVersion { get; }

    public int DiscoveryHResult { get; }

    public int FileVersionHResult { get; }

    public uint LoadFlags { get; }

    public int LoadHResult { get; }

    public int GetProcAddressHResult { get; }

    public int Win32Error { get; }

    public GameInputRuntimeCandidate WithProbeResult(GameInputRuntimeCandidateInfo info)
    {
        return new GameInputRuntimeCandidate(
            ModuleKind,
            info.ModulePath,
            Exists,
            info.FileVersion,
            DiscoveryHResult,
            FileVersionHResult,
            LoadFlags,
            info.LoadHResult,
            info.GetProcAddressHResult,
            info.Win32Error);
    }

    public GameInputRuntimeCandidateInfo ToInfo()
    {
        return ToInfo(ModulePath, LoadHResult, GetProcAddressHResult, Win32Error);
    }

    public GameInputRuntimeCandidateInfo ToInfo(int loadHResult, int getProcAddressHResult, int win32Error)
    {
        return ToInfo(ModulePath, loadHResult, getProcAddressHResult, win32Error);
    }

    public GameInputRuntimeCandidateInfo ToInfo(string modulePath, int loadHResult, int getProcAddressHResult, int win32Error)
    {
        return new GameInputRuntimeCandidateInfo(
            ModuleKind,
            modulePath,
            Exists,
            FileVersion,
            DiscoveryHResult,
            FileVersionHResult,
            loadHResult,
            getProcAddressHResult,
            win32Error);
    }
}
