using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A GameInput input mapper wrapper.
/// GameInput mapper 包裝。
/// </summary>
public sealed class GameInputMapper : IDisposable
{
    private static readonly int s_axisMappingSize = Marshal.SizeOf<GameInputAxisMapping>();
    private static readonly int s_buttonMappingSize = Marshal.SizeOf<GameInputButtonMapping>();

    private readonly GameInputComHandle _handle;

    internal GameInputMapper(IGameInputMapper native)
    {
        _handle = new GameInputComHandle(native.Pointer);
    }

    /// <summary>
    /// Tries to get the gamepad axis mapping information.
    /// 嘗試取得 gamepad 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">The axis element whose mapping is queried. 要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the axis mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public unsafe bool TryGetGamepadAxisMappingInfo(GameInputGamepadAxes axisElement, out GameInputAxisMapping mapping)
    {
        byte* buffer = stackalloc byte[s_axisMappingSize];
        using ComLease<IGameInputMapper> call = EnterNative();
        return ReadMapping(call.Native.GetGamepadAxisMappingInfo(axisElement, (IntPtr)buffer), buffer, out mapping);
    }

    /// <summary>
    /// Tries to get the gamepad button mapping information.
    /// 嘗試取得 gamepad 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public unsafe bool TryGetGamepadButtonMappingInfo(GameInputGamepadButtons buttonElement, out GameInputButtonMapping mapping)
    {
        byte* buffer = stackalloc byte[s_buttonMappingSize];
        using ComLease<IGameInputMapper> call = EnterNative();
        return ReadMapping(call.Native.GetGamepadButtonMappingInfo(buttonElement, (IntPtr)buffer), buffer, out mapping);
    }

    /// <summary>
    /// Tries to get the flight stick axis mapping information.
    /// 嘗試取得 flight stick 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">The axis element whose mapping is queried. 要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the axis mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public unsafe bool TryGetFlightStickAxisMappingInfo(GameInputFlightStickAxes axisElement, out GameInputAxisMapping mapping)
    {
        byte* buffer = stackalloc byte[s_axisMappingSize];
        using ComLease<IGameInputMapper> call = EnterNative();
        return ReadMapping(call.Native.GetFlightStickAxisMappingInfo(axisElement, (IntPtr)buffer), buffer, out mapping);
    }

    /// <summary>
    /// Tries to get the flight stick button mapping information.
    /// 嘗試取得 flight stick 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public unsafe bool TryGetFlightStickButtonMappingInfo(GameInputFlightStickButtons buttonElement, out GameInputButtonMapping mapping)
    {
        byte* buffer = stackalloc byte[s_buttonMappingSize];
        using ComLease<IGameInputMapper> call = EnterNative();
        return ReadMapping(call.Native.GetFlightStickButtonMappingInfo(buttonElement, (IntPtr)buffer), buffer, out mapping);
    }

    /// <summary>
    /// Tries to get the racing wheel axis mapping information.
    /// 嘗試取得 racing wheel 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">The axis element whose mapping is queried. 要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the axis mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public unsafe bool TryGetRacingWheelAxisMappingInfo(GameInputRacingWheelAxes axisElement, out GameInputAxisMapping mapping)
    {
        byte* buffer = stackalloc byte[s_axisMappingSize];
        using ComLease<IGameInputMapper> call = EnterNative();
        return ReadMapping(call.Native.GetRacingWheelAxisMappingInfo(axisElement, (IntPtr)buffer), buffer, out mapping);
    }

    /// <summary>
    /// Tries to get the racing wheel button mapping information.
    /// 嘗試取得 racing wheel 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public unsafe bool TryGetRacingWheelButtonMappingInfo(GameInputRacingWheelButtons buttonElement, out GameInputButtonMapping mapping)
    {
        byte* buffer = stackalloc byte[s_buttonMappingSize];
        using ComLease<IGameInputMapper> call = EnterNative();
        return ReadMapping(call.Native.GetRacingWheelButtonMappingInfo(buttonElement, (IntPtr)buffer), buffer, out mapping);
    }

    /// <summary>
    /// Tries to get the arcade stick button mapping information.
    /// 嘗試取得 arcade stick 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public unsafe bool TryGetArcadeStickButtonMappingInfo(GameInputArcadeStickButtons buttonElement, out GameInputButtonMapping mapping)
    {
        byte* buffer = stackalloc byte[s_buttonMappingSize];
        using ComLease<IGameInputMapper> call = EnterNative();
        return ReadMapping(call.Native.GetArcadeStickButtonMappingInfo(buttonElement, (IntPtr)buffer), buffer, out mapping);
    }

    /// <summary>
    /// Releases the COM reference held by the input mapper wrapper.
    /// 釋放 input mapper 包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        // SafeHandle.Dispose 本身冪等且執行緒安全，重複或並行呼叫只會釋放一次。
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }

#if NET8_0_OR_GREATER
    private static unsafe bool ReadMapping<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(bool found, byte* buffer, out T mapping)
        where T : struct
#else
    private static unsafe bool ReadMapping<T>(bool found, byte* buffer, out T mapping)
        where T : struct
#endif
    {
        // mapping 結構含有 bool 欄位，透過封送讀取以確保與原生配置一致；緩衝區由呼叫端以 stackalloc 提供，不配置原生堆積。
        if (!found)
        {
            mapping = default;
            return false;
        }

        mapping = Marshal.PtrToStructure<T>((IntPtr)buffer);
        return true;
    }

    internal ComLease<IGameInputMapper> EnterNative()
    {
        return _handle.Acquire<IGameInputMapper>(nameof(GameInputMapper));
    }
}
