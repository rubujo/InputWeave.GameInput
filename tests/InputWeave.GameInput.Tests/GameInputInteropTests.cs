using System.Reflection;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputInteropTests
{
    [TestMethod]
    public void GeneratedEnumsIncludeGameInputV3Members()
    {
        object? apiVersion = typeof(GameInputConstants).GetField(nameof(GameInputConstants.ApiVersion))?.GetRawConstantValue();
        GameInputKind gamepad = Enum.Parse<GameInputKind>("GameInputKindGamepad");
        GameInputKind controller = Enum.Parse<GameInputKind>("GameInputKindController");
        GameInputRawDeviceReportKind rawOutput = Enum.Parse<GameInputRawDeviceReportKind>("GameInputRawOutputReport");
        GameInputDeviceStatus anyStatus = Enum.Parse<GameInputDeviceStatus>("GameInputDeviceAnyStatus");

        Assert.AreEqual(3, apiVersion);
        Assert.AreEqual(GameInputKind.GameInputKindUnknown, gamepad & controller);
        Assert.AreEqual(1, (int)rawOutput);
        Assert.AreEqual(unchecked((int)0xFFFFFFFF), (int)anyStatus);
    }

    [TestMethod]
    public void BlittableStructSizesMatchNativeHeaderForCommonStates()
    {
        Assert.AreEqual(32, Marshal.SizeOf<AppLocalDeviceId>());
        Assert.AreEqual(28, Marshal.SizeOf<GameInputGamepadState>());
        Assert.AreEqual(56, Marshal.SizeOf<GameInputMouseState>());
        Assert.AreEqual(12, Marshal.SizeOf<GameInputKeyState>());
        Assert.AreEqual(12, Marshal.SizeOf<GameInputRawDeviceReportInfo>());
        Assert.AreEqual(20, Marshal.SizeOf<GameInputAxisMapping>());
        Assert.AreEqual(16, Marshal.SizeOf<GameInputButtonMapping>());
        Assert.AreEqual(112, Marshal.SizeOf<GameInputForceFeedbackParams>());
        Assert.AreEqual(16, Marshal.SizeOf<GameInputForceFeedbackMotorInfo>());
        Assert.AreEqual(12, Marshal.SizeOf<GameInputMouseInfo>());
    }

    [TestMethod]
    public void NativeBoolFieldsUseSingleByteOffsets()
    {
        Assert.AreEqual(8, Marshal.OffsetOf<GameInputAxisMapping>(nameof(GameInputAxisMapping.IsInverted)).ToInt32());
        Assert.AreEqual(9, Marshal.OffsetOf<GameInputAxisMapping>(nameof(GameInputAxisMapping.FromTwoButtons)).ToInt32());
        Assert.AreEqual(8, Marshal.OffsetOf<GameInputButtonMapping>(nameof(GameInputButtonMapping.IsInverted)).ToInt32());
        Assert.AreEqual(8, Marshal.OffsetOf<GameInputMouseInfo>(nameof(GameInputMouseInfo.HasWheelX)).ToInt32());
        Assert.AreEqual(9, Marshal.OffsetOf<GameInputMouseInfo>(nameof(GameInputMouseInfo.HasWheelY)).ToInt32());
    }

    [TestMethod]
    public void GeneratedCallbackDelegatesUseWinapiCallingConvention()
    {
        Type[] callbackTypes =
        [
                typeof(GameInputReadingCallback),
                typeof(GameInputDeviceCallback),
                typeof(GameInputSystemButtonCallback),
                typeof(GameInputKeyboardLayoutCallback)
            ];

        foreach (Type callbackType in callbackTypes)
        {
            UnmanagedFunctionPointerAttribute? attribute = callbackType.GetCustomAttribute<UnmanagedFunctionPointerAttribute>();

            Assert.IsNotNull(attribute, $"{callbackType.Name} 應明確標示 unmanaged calling convention。");
            Assert.AreEqual(CallingConvention.Winapi, attribute.CallingConvention);
        }
    }

    [TestMethod]
    public void GeneratedComInterfacesUseIUnknownAndPreserveSig()
    {
        Type[] interfaceTypes =
        [
                typeof(IGameInput),
                typeof(IGameInputRawDeviceReport),
                typeof(IGameInputReading),
                typeof(IGameInputDevice),
                typeof(IGameInputDispatcher),
                typeof(IGameInputForceFeedbackEffect),
                typeof(IGameInputMapper)
            ];

        foreach (Type interfaceType in interfaceTypes)
        {
            InterfaceTypeAttribute? interfaceTypeAttribute = interfaceType.GetCustomAttribute<InterfaceTypeAttribute>();
            MethodInfo[] methods = interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.IsTrue(interfaceType.IsImport, $"{interfaceType.Name} 應由 [ComImport] 產生。");
            Assert.IsNotNull(interfaceType.GetCustomAttribute<GuidAttribute>(), $"{interfaceType.Name} 應有 IID。");
            Assert.IsNotNull(interfaceTypeAttribute, $"{interfaceType.Name} 應標示 COM interface type。");
            Assert.AreEqual(ComInterfaceType.InterfaceIsIUnknown, interfaceTypeAttribute.Value);
            Assert.IsNotEmpty(methods, $"{interfaceType.Name} 應包含原生 vtable 方法。");

            foreach (MethodInfo method in methods)
            {
                Assert.IsNotNull(method.GetCustomAttribute<PreserveSigAttribute>(), $"{interfaceType.Name}.{method.Name} 應保留 HRESULT/原生回傳語意。");
            }
        }
    }

    [TestMethod]
    public void NativeDllImportsConstrainSearchPaths()
    {
        var methods = typeof(GameInputConstants).Assembly.GetTypes()
            .SelectMany(static type => type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Select(static method => new
            {
                Method = method,
                Import = method.GetCustomAttribute<DllImportAttribute>()
            })
            .Where(static item => item.Import is not null)
            .ToArray();

        Assert.IsNotEmpty(methods);

        foreach (var item in methods)
        {
            DefaultDllImportSearchPathsAttribute? attribute = item.Method.GetCustomAttribute<DefaultDllImportSearchPathsAttribute>();
            string methodName = $"{item.Method.DeclaringType?.FullName}.{item.Method.Name}";

            Assert.IsNotNull(attribute, $"{methodName} 應明確限制 DLL 搜尋路徑。");
            Assert.AreEqual(DllImportSearchPath.System32, attribute.Paths & DllImportSearchPath.System32, $"{methodName} 應限制從 System32 載入 native DLL。");
        }
    }

    [TestMethod]
    public void SourceNativeImportsConstrainSearchPaths()
    {
        AssertNativeImportDeclarationsUseSystem32SearchPath(
            "src/InputWeave.GameInput/Win32NativeMethods.Net10.cs",
            "[LibraryImport(");

        AssertNativeImportDeclarationsUseSystem32SearchPath(
            "src/InputWeave.GameInput/Win32NativeMethods.NetFramework.cs",
            "[DllImport(");
    }

    [TestMethod]
    public void GameInputInitializeUsesManagedRuntimeLoader()
    {
        AssertGameInputInitializeDelegatesToRuntimeLoader("src/InputWeave.GameInput/Interop/GameInputNativeMethods.Net10.cs");
        AssertGameInputInitializeDelegatesToRuntimeLoader("src/InputWeave.GameInput/Interop/GameInputNativeMethods.NetFramework.cs");
    }

    [TestMethod]
    public void GameInputExceptionFormatsKnownHResults()
    {
        GameInputException exception = new(GameInputHResult.ReadingNotFound);

        Assert.AreEqual(GameInputHResult.ReadingNotFound, exception.HResult);
        Assert.IsTrue(exception.IsNotFound);
        Assert.Contains("讀取資料", exception.Message);
    }

    private static void AssertNativeImportDeclarationsUseSystem32SearchPath(string relativePath, string importAttributePrefix)
    {
        string sourcePath = Path.Combine(FindRepositoryRoot(), relativePath);
        string source = File.ReadAllText(sourcePath);
        int importIndex = source.IndexOf(importAttributePrefix, StringComparison.Ordinal);

        Assert.AreNotEqual(-1, importIndex, $"{relativePath} 應包含 {importAttributePrefix} 宣告。");

        while (importIndex >= 0)
        {
            int declarationEnd = source.IndexOf(';', importIndex);

            Assert.AreNotEqual(-1, declarationEnd, $"{relativePath} 的 native import 宣告應以分號結尾。");

            string declaration = source[importIndex..declarationEnd];

            Assert.Contains("DefaultDllImportSearchPaths(DllImportSearchPath.System32)", declaration);

            importIndex = source.IndexOf(importAttributePrefix, declarationEnd, StringComparison.Ordinal);
        }
    }

    private static void AssertGameInputInitializeDelegatesToRuntimeLoader(string relativePath)
    {
        string sourcePath = Path.Combine(FindRepositoryRoot(), relativePath);
        string source = File.ReadAllText(sourcePath);

        Assert.Contains("GameInputRuntimeLoader.GameInputInitialize", source);
        Assert.IsFalse(
            source.Contains("LibraryImport(GameInputConstants.DllName", StringComparison.Ordinal),
            $"{relativePath} 不應直接 LibraryImport GameInput.dll。");
        Assert.IsFalse(
            source.Contains("DllImport(GameInputConstants.DllName", StringComparison.Ordinal),
            $"{relativePath} 不應直接 DllImport GameInput.dll。");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InputWeave.GameInput.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("找不到 InputWeave.GameInput.slnx，無法定位 repo root。");
        return string.Empty;
    }
}
