namespace InputWeave.GameInput;

/// <summary>
/// 提供 GameInput runtime 載入狀態與探測功能。
/// </summary>
public static class GameInputRuntime
{
    /// <summary>
    /// 取得目前 GameInput runtime 載入結果。
    /// </summary>
    /// <exception cref="DllNotFoundException">找不到可用的 GameInput runtime。</exception>
    /// <exception cref="EntryPointNotFoundException">找到的 GameInput runtime 缺少必要進入點。</exception>
    /// <returns>目前 GameInput runtime 載入結果。</returns>
    public static GameInputRuntimeInfo GetInfo()
    {
        return GameInputRuntimeLoader.GetInfo();
    }

    /// <summary>
    /// 嘗試探測目前可用的 GameInput runtime。
    /// </summary>
    /// <param name="info">探測完成後的 GameInput runtime 狀態。</param>
    /// <returns>如果找到可用的 GameInput runtime，則為 <see langword="true" />；否則為 <see langword="false" />。</returns>
    public static bool TryProbe(out GameInputRuntimeProbeInfo info)
    {
        return GameInputRuntimeLoader.TryProbe(out info);
    }
}
