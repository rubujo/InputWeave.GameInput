namespace InputWeave.GameInput;

/// <summary>
/// A scope that clears the GameInput rumble state when disposed.
/// 釋放時清除 GameInput rumble 狀態的 scope。
/// </summary>
public sealed class GameInputRumbleScope : IDisposable
{
    private Action? _clearRumbleState;

    internal GameInputRumbleScope(Action clearRumbleState)
    {
        _clearRumbleState = clearRumbleState;
    }

    /// <summary>
    /// Whether this scope has been disposed.
    /// 此 scope 是否已釋放。
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Clears the rumble state and ends this scope.
    /// 清除 rumble 狀態並結束此 scope。
    /// </summary>
    /// <remarks>
    /// Clearing may fail when the device has been disconnected or disposed; following the convention that
    /// <see cref="IDisposable.Dispose"/> should not throw, <see cref="GameInputException"/> and
    /// <see cref="ObjectDisposedException"/> are swallowed here so the caller's disposal flow is not interrupted.
    /// 若裝置已中斷連線或已被釋放，清除動作可能失敗；依 <see cref="IDisposable.Dispose"/> 不應拋出例外的慣例，
    /// 此處會吞下 <see cref="GameInputException"/> 與 <see cref="ObjectDisposedException"/>，不中斷呼叫端的釋放流程。
    /// </remarks>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        Action? clear = _clearRumbleState;
        _clearRumbleState = null;
        if (clear is null)
        {
            return;
        }

        try
        {
            clear();
        }
        catch (GameInputException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }
}
