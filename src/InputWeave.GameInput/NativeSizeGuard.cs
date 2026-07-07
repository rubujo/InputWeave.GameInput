namespace InputWeave.GameInput;

/// <summary>
/// Centralizes validation of counts and sizes reported by native GameInput, avoiding trusting anomalous values to allocate memory
/// of the corresponding size.
/// 集中驗證原生 GameInput 回報的數量與大小，避免信任異常值直接配置對應大小的記憶體。
/// </summary>
internal static class NativeSizeGuard
{
    /// <summary>
    /// The maximum native-reported input element count (axes, buttons, switches, keys, indexes, report descriptions, and so on).
    /// Actual device element counts are far smaller than this limit; exceeding it is treated as an anomalous device or driver
    /// report.
    /// 原生回報的輸入元素數量上限（軸、按鈕、開關、按鍵、索引、報告描述等）。
    /// 實際裝置的元素數量遠小於這個上限；超過時視為裝置或驅動程式回報異常。
    /// </summary>
    internal const int MaxElementCount = 4096;

    /// <summary>
    /// The maximum native-reported UTF-8 string byte length (display names, PnP paths, and so on). Actual device strings are far
    /// shorter than this limit; exceeding it is treated as an anomalous device or driver report.
    /// 原生回報的 UTF-8 字串位元組長度上限（顯示名稱、PnP 路徑等）。
    /// 實際裝置字串遠短於這個上限；超過時視為裝置或驅動程式回報異常。
    /// </summary>
    internal const int MaxUtf8StringByteLength = 4096;

    /// <summary>
    /// Validates that the native-reported count or size does not exceed the limit, then returns it converted to
    /// <see cref="int"/>.
    /// 驗證原生回報的數量或大小不超過上限，通過後轉為 <see cref="int"/> 傳回。
    /// </summary>
    /// <param name="count">The count or size reported by the native side. 原生回報的數量或大小。</param>
    /// <param name="maxCount">The allowed upper limit. 允許的上限。</param>
    /// <param name="description">The description of what the value represents, used in the error message. 數值的用途描述，供錯誤訊息使用。</param>
    /// <returns>The validated value. 驗證通過的數值。</returns>
    /// <exception cref="InvalidOperationException"><paramref name="count"/> exceeds <paramref name="maxCount"/>. <paramref name="count"/> 超過 <paramref name="maxCount"/>。</exception>
    internal static int EnsureCount(ulong count, int maxCount, string description)
    {
        if (count > (ulong)maxCount)
        {
            throw new InvalidOperationException($"原生回報的{description}（{count}）超過上限（{maxCount}），視為裝置或驅動程式回報異常。");
        }

        return (int)count;
    }
}
