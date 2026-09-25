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
    public bool TryGetGamepadAxisMappingInfo(GameInputGamepadAxes axisElement, out GameInputAxisMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputAxisMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            using ComLease<IGameInputMapper> call = EnterNative();
            return call.Native.GetGamepadAxisMappingInfo(axisElement, pointer);
        }
    }

    /// <summary>
    /// Tries to get the gamepad button mapping information.
    /// 嘗試取得 gamepad 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetGamepadButtonMappingInfo(GameInputGamepadButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            using ComLease<IGameInputMapper> call = EnterNative();
            return call.Native.GetGamepadButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <summary>
    /// Tries to get the flight stick axis mapping information.
    /// 嘗試取得 flight stick 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">The axis element whose mapping is queried. 要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the axis mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetFlightStickAxisMappingInfo(GameInputFlightStickAxes axisElement, out GameInputAxisMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputAxisMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            using ComLease<IGameInputMapper> call = EnterNative();
            return call.Native.GetFlightStickAxisMappingInfo(axisElement, pointer);
        }
    }

    /// <summary>
    /// Tries to get the flight stick button mapping information.
    /// 嘗試取得 flight stick 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetFlightStickButtonMappingInfo(GameInputFlightStickButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            using ComLease<IGameInputMapper> call = EnterNative();
            return call.Native.GetFlightStickButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <summary>
    /// Tries to get the racing wheel axis mapping information.
    /// 嘗試取得 racing wheel 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">The axis element whose mapping is queried. 要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the axis mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetRacingWheelAxisMappingInfo(GameInputRacingWheelAxes axisElement, out GameInputAxisMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputAxisMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            using ComLease<IGameInputMapper> call = EnterNative();
            return call.Native.GetRacingWheelAxisMappingInfo(axisElement, pointer);
        }
    }

    /// <summary>
    /// Tries to get the racing wheel button mapping information.
    /// 嘗試取得 racing wheel 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetRacingWheelButtonMappingInfo(GameInputRacingWheelButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            using ComLease<IGameInputMapper> call = EnterNative();
            return call.Native.GetRacingWheelButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <summary>
    /// Tries to get the arcade stick button mapping information.
    /// 嘗試取得 arcade stick 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">The button element whose mapping is queried. 要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">The output field that receives the mapping information. 接收 mapping 資訊的輸出欄位。</param>
    /// <returns>Returns <see langword="true"/> and outputs the result through <paramref name="mapping"/> when the button mapping information is retrieved successfully; returns <see langword="false"/> when the query fails. 若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetArcadeStickButtonMappingInfo(GameInputArcadeStickButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            using ComLease<IGameInputMapper> call = EnterNative();
            return call.Native.GetArcadeStickButtonMappingInfo(buttonElement, pointer);
        }
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

#if NET10_0_OR_GREATER
    private static bool TryReadMapping<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(int size, Func<IntPtr, bool> read, out T mapping)
        where T : struct
#else
    private static bool TryReadMapping<T>(int size, Func<IntPtr, bool> read, out T mapping)
        where T : struct
#endif
    {
        IntPtr pointer = Marshal.AllocHGlobal(size);
        try
        {
            if (!read(pointer))
            {
                mapping = default;
                return false;
            }

            mapping = Marshal.PtrToStructure<T>(pointer);
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    internal ComLease<IGameInputMapper> EnterNative()
    {
        return _handle.Acquire<IGameInputMapper>(nameof(GameInputMapper));
    }
}
