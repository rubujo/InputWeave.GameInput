namespace InputWeave.GameInput;

/// <summary>
/// Represents the GameInput runtime probe result.
/// 表示 GameInput runtime 探測結果。
/// </summary>
public readonly struct GameInputRuntimeProbeInfo
{
    internal GameInputRuntimeProbeInfo(
        string loaderPolicy,
        bool isAvailable,
        GameInputRuntimeModuleKind selectedModuleKind,
        string selectedModulePath,
        Version? selectedFileVersion,
        int hResult,
        int win32Error,
        IReadOnlyList<GameInputRuntimeCandidateInfo> candidates)
    {
        LoaderPolicy = loaderPolicy;
        IsAvailable = isAvailable;
        SelectedModuleKind = selectedModuleKind;
        SelectedModulePath = selectedModulePath;
        SelectedFileVersion = selectedFileVersion;
        HResult = hResult;
        Win32Error = win32Error;
        Candidates = candidates;
    }

    /// <summary>
    /// Gets the name of the GameInput runtime loading policy currently in use.
    /// 取得目前使用的 GameInput runtime 載入策略名稱。
    /// </summary>
    public string LoaderPolicy { get; }

    /// <summary>
    /// Gets whether a usable GameInput runtime was found.
    /// 取得是否找到可用的 GameInput runtime。
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    /// Gets the source of the finally selected GameInput runtime module.
    /// 取得最後選擇的 GameInput runtime 模組來源。
    /// </summary>
    public GameInputRuntimeModuleKind SelectedModuleKind { get; }

    /// <summary>
    /// Gets the full path of the finally selected GameInput runtime module.
    /// 取得最後選擇的 GameInput runtime 模組完整路徑。
    /// </summary>
    public string SelectedModulePath { get; }

    /// <summary>
    /// Gets the file version of the finally selected GameInput runtime.
    /// 取得最後選擇的 GameInput runtime 檔案版本。
    /// </summary>
    public Version? SelectedFileVersion { get; }

    /// <summary>
    /// Gets the final HRESULT of the probing flow.
    /// 取得探測流程的最終 HRESULT。
    /// </summary>
    public int HResult { get; }

    /// <summary>
    /// Gets the final Win32 error code of the probing flow.
    /// 取得探測流程的最終 Win32 錯誤碼。
    /// </summary>
    public int Win32Error { get; }

    /// <summary>
    /// Gets the list of candidate GameInput runtime modules from this probe.
    /// 取得本次探測的候選 GameInput runtime 模組清單。
    /// </summary>
    public IReadOnlyList<GameInputRuntimeCandidateInfo> Candidates { get; }
}
