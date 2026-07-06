namespace InputWeave.GameInput;

/// <summary>
/// 共用的雜湊碼組合輔助方法，供 Snapshot 型別的 <see cref="object.GetHashCode"/> 覆寫使用；
/// 兩個 TFM 共用同一份程式碼（不依賴 <c>System.HashCode</c>，因為 <c>net48</c> 不支援）。
/// </summary>
internal static class HashCodeCombiner
{
    /// <summary>
    /// 把一個雜湊碼組合進目前的種子值。
    /// </summary>
    /// <param name="seed">目前的雜湊碼種子值。</param>
    /// <param name="value">要組合進去的雜湊碼。</param>
    /// <returns>組合後的雜湊碼。</returns>
    public static int Combine(int seed, int value)
    {
        unchecked
        {
            return (seed * 397) ^ value;
        }
    }

    /// <summary>
    /// 依序把一組值的雜湊碼組合進目前的種子值。
    /// </summary>
    /// <typeparam name="T">元素型別。</typeparam>
    /// <param name="seed">目前的雜湊碼種子值。</param>
    /// <param name="values">要逐一組合的元素集合。</param>
    /// <returns>組合後的雜湊碼。</returns>
    public static int CombineRange<T>(int seed, IEnumerable<T> values)
    {
        int hash = seed;
        foreach (T value in values)
        {
            hash = Combine(hash, value?.GetHashCode() ?? 0);
        }

        return hash;
    }
}
