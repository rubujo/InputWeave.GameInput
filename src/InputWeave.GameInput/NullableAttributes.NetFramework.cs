namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// .NET Framework 的參考組件沒有這個屬性；C# 編譯器依名稱辨識，因此以內部型別補上，讓 net48 的
/// 可為 null 流程分析與 net10 一致。
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
    /// <summary>
    /// 方法回傳此值時，參數不為 null。
    /// </summary>
    public bool ReturnValue { get; } = returnValue;
}
