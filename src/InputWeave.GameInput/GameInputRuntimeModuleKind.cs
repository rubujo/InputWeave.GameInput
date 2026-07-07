namespace InputWeave.GameInput;

/// <summary>
/// The source kind of a GameInput runtime module.
/// GameInput runtime 模組來源種類。
/// </summary>
public enum GameInputRuntimeModuleKind
{
    /// <summary>
    /// No usable GameInput runtime was found.
    /// 未找到可用的 GameInput runtime。
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// The inbox GameInput.dll inside Windows System32.
    /// Windows System32 內的 inbox GameInput.dll。
    /// </summary>
    SystemGameInput = 1,

    /// <summary>
    /// The GameInputRedist.dll inside Windows System32.
    /// Windows System32 內的 GameInputRedist.dll。
    /// </summary>
    SystemGameInputRedist = 2,

    /// <summary>
    /// The GameInputRedist.dll from the Microsoft GameInput redist registry path.
    /// Microsoft GameInput redist 登錄檔路徑中的 GameInputRedist.dll。
    /// </summary>
    RegistryGameInputRedist = 3
}
