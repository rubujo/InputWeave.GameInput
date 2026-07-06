using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput mapper 包裝。
/// </summary>
public sealed class GameInputMapper : IDisposable
{
    private IGameInputMapper? _native;
    private bool _disposed;

    internal GameInputMapper(IGameInputMapper native)
    {
        _native = native;
    }

    /// <summary>
    /// 嘗試取得 gamepad 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">接收 mapping 資訊的輸出欄位。</param>
    /// <returns>若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetGamepadAxisMappingInfo(GameInputGamepadAxes axisElement, out GameInputAxisMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputAxisMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetGamepadAxisMappingInfo(axisElement, pointer);
        }
    }

    /// <summary>
    /// 嘗試取得 gamepad 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">接收 mapping 資訊的輸出欄位。</param>
    /// <returns>若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetGamepadButtonMappingInfo(GameInputGamepadButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetGamepadButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <summary>
    /// 嘗試取得 flight stick 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">接收 mapping 資訊的輸出欄位。</param>
    /// <returns>若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetFlightStickAxisMappingInfo(GameInputFlightStickAxes axisElement, out GameInputAxisMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputAxisMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetFlightStickAxisMappingInfo(axisElement, pointer);
        }
    }

    /// <summary>
    /// 嘗試取得 flight stick 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">接收 mapping 資訊的輸出欄位。</param>
    /// <returns>若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetFlightStickButtonMappingInfo(GameInputFlightStickButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetFlightStickButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <summary>
    /// 嘗試取得 racing wheel 軸對應資訊。
    /// </summary>
    /// <param name="axisElement">要查詢 mapping 的 axis 元素。</param>
    /// <param name="mapping">接收 mapping 資訊的輸出欄位。</param>
    /// <returns>若成功取得對應的軸 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetRacingWheelAxisMappingInfo(GameInputRacingWheelAxes axisElement, out GameInputAxisMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputAxisMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetRacingWheelAxisMappingInfo(axisElement, pointer);
        }
    }

    /// <summary>
    /// 嘗試取得 racing wheel 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">接收 mapping 資訊的輸出欄位。</param>
    /// <returns>若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetRacingWheelButtonMappingInfo(GameInputRacingWheelButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetRacingWheelButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <summary>
    /// 嘗試取得 arcade stick 按鈕對應資訊。
    /// </summary>
    /// <param name="buttonElement">要查詢 mapping 的 button 元素。</param>
    /// <param name="mapping">接收 mapping 資訊的輸出欄位。</param>
    /// <returns>若成功取得對應的按鈕 mapping 資訊，傳回 <see langword="true"/> 並透過 <paramref name="mapping"/> 輸出結果；查詢失敗時傳回 <see langword="false"/>。</returns>
    public bool TryGetArcadeStickButtonMappingInfo(GameInputArcadeStickButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetArcadeStickButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <summary>
    /// 釋放 input mapper 包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_native is not null)
        {
#if NET10_0_OR_GREATER
            _native.Value.Release();
#else
            Marshal.ReleaseComObject(_native);
#endif
            _native = null;
        }

        _disposed = true;
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

    private IGameInputMapper Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputMapper))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputMapper));
        }
    }
}
