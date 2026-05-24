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
    public bool TryGetArcadeStickButtonMappingInfo(GameInputArcadeStickButtons buttonElement, out GameInputButtonMapping mapping)
    {
        return TryReadMapping(size: Marshal.SizeOf<GameInputButtonMapping>(), NativeMapping, out mapping);

        bool NativeMapping(IntPtr pointer)
        {
            return Native.GetArcadeStickButtonMappingInfo(buttonElement, pointer);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_native is not null)
        {
            Marshal.ReleaseComObject(_native);
            _native = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static bool TryReadMapping<T>(int size, Func<IntPtr, bool> read, out T mapping)
        where T : struct
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
