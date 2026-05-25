namespace InputWeave.GameInput;

/// <summary>
/// 表示目前 GameInput runtime 載入結果。
/// </summary>
public readonly struct GameInputRuntimeInfo
{
    internal GameInputRuntimeInfo(
        string loaderPolicy,
        GameInputRuntimeModuleKind loadedModuleKind,
        string loadedModulePath,
        Version? fileVersion)
    {
        LoaderPolicy = loaderPolicy;
        LoadedModuleKind = loadedModuleKind;
        LoadedModulePath = loadedModulePath;
        FileVersion = fileVersion;
    }

    /// <summary>
    /// 取得目前使用的 GameInput runtime 載入策略名稱。
    /// </summary>
    public string LoaderPolicy { get; }

    /// <summary>
    /// 取得已載入的 GameInput runtime 模組來源。
    /// </summary>
    public GameInputRuntimeModuleKind LoadedModuleKind { get; }

    /// <summary>
    /// 取得已載入的 GameInput runtime 模組完整路徑。
    /// </summary>
    public string LoadedModulePath { get; }

    /// <summary>
    /// 取得已載入 GameInput runtime 的檔案版本。
    /// </summary>
    public Version? FileVersion { get; }

    /// <summary>
    /// 取得是否已找到可用的 GameInput runtime。
    /// </summary>
    public bool IsAvailable
    {
        get
        {
            return LoadedModuleKind != GameInputRuntimeModuleKind.Unavailable;
        }
    }
}
