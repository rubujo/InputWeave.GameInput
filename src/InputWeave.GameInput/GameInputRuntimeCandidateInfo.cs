namespace InputWeave.GameInput;

/// <summary>
/// Represents the probe result of one GameInput runtime candidate module.
/// 表示一次 GameInput runtime 候選模組探測結果。
/// </summary>
public readonly struct GameInputRuntimeCandidateInfo
{
    internal GameInputRuntimeCandidateInfo(
        GameInputRuntimeModuleKind moduleKind,
        string modulePath,
        bool exists,
        Version? fileVersion,
        int discoveryHResult,
        int fileVersionHResult,
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
        LoadHResult = loadHResult;
        GetProcAddressHResult = getProcAddressHResult;
        Win32Error = win32Error;
    }

    /// <summary>
    /// Gets the source of the candidate GameInput runtime module.
    /// 取得候選 GameInput runtime 模組來源。
    /// </summary>
    public GameInputRuntimeModuleKind ModuleKind { get; }

    /// <summary>
    /// Gets the full path of the candidate GameInput runtime module.
    /// 取得候選 GameInput runtime 模組完整路徑。
    /// </summary>
    public string ModulePath { get; }

    /// <summary>
    /// Gets whether the candidate file exists.
    /// 取得候選檔案是否存在。
    /// </summary>
    public bool Exists { get; }

    /// <summary>
    /// Gets the candidate file version.
    /// 取得候選檔案版本。
    /// </summary>
    public Version? FileVersion { get; }

    /// <summary>
    /// Gets the HRESULT of the candidate path discovery.
    /// 取得候選路徑探索結果 HRESULT。
    /// </summary>
    public int DiscoveryHResult { get; }

    /// <summary>
    /// Gets the HRESULT of the candidate file version query.
    /// 取得候選檔案版本查詢結果 HRESULT。
    /// </summary>
    public int FileVersionHResult { get; }

    /// <summary>
    /// Gets the HRESULT of the candidate module load.
    /// 取得候選模組載入結果 HRESULT。
    /// </summary>
    public int LoadHResult { get; }

    /// <summary>
    /// Gets the HRESULT of the candidate module's GameInputInitialize entry point lookup.
    /// 取得候選模組 GameInputInitialize 進入點查詢結果 HRESULT。
    /// </summary>
    public int GetProcAddressHResult { get; }

    /// <summary>
    /// Gets the Win32 error code of the candidate module load or entry point lookup.
    /// 取得候選模組載入或進入點查詢的 Win32 錯誤碼。
    /// </summary>
    public int Win32Error { get; }
}
