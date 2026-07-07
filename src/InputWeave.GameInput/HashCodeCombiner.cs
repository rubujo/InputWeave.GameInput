namespace InputWeave.GameInput;

/// <summary>
/// The shared hash code combining helper used by the <see cref="object.GetHashCode"/> overrides of the snapshot types; both TFMs
/// share the same code (it does not depend on <c>System.HashCode</c>, which <c>net48</c> lacks).
/// 共用的雜湊碼組合輔助方法，供 Snapshot 型別的 <see cref="object.GetHashCode"/> 覆寫使用；
/// 兩個 TFM 共用同一份程式碼（不依賴 <c>System.HashCode</c>，因為 <c>net48</c> 不支援）。
/// </summary>
internal static class HashCodeCombiner
{
    /// <summary>
    /// Combines one hash code into the current seed value.
    /// 把一個雜湊碼組合進目前的種子值。
    /// </summary>
    /// <param name="seed">The current hash code seed value. 目前的雜湊碼種子值。</param>
    /// <param name="value">The hash code to combine. 要組合進去的雜湊碼。</param>
    /// <returns>The combined hash code. 組合後的雜湊碼。</returns>
    public static int Combine(int seed, int value)
    {
        unchecked
        {
            return (seed * 397) ^ value;
        }
    }

    /// <summary>
    /// Combines the hash codes of a sequence of values into the current seed value one by one.
    /// 依序把一組值的雜湊碼組合進目前的種子值。
    /// </summary>
    /// <typeparam name="T">The element type. 元素型別。</typeparam>
    /// <param name="seed">The current hash code seed value. 目前的雜湊碼種子值。</param>
    /// <param name="values">The collection of elements to combine one by one. 要逐一組合的元素集合。</param>
    /// <returns>The combined hash code. 組合後的雜湊碼。</returns>
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
