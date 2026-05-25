namespace InputWeave.GameInput;

/// <summary>
/// GameInput runtime 模組來源種類。
/// </summary>
public enum GameInputRuntimeModuleKind
{
    /// <summary>
    /// 未找到可用的 GameInput runtime。
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// Windows System32 內的 inbox GameInput.dll。
    /// </summary>
    SystemGameInput = 1,

    /// <summary>
    /// Windows System32 內的 GameInputRedist.dll。
    /// </summary>
    SystemGameInputRedist = 2,

    /// <summary>
    /// Microsoft GameInput redist 登錄檔路徑中的 GameInputRedist.dll。
    /// </summary>
    RegistryGameInputRedist = 3
}
