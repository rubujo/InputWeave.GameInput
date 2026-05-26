namespace InputWeave.GameInput;

/// <summary>
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
    /// 清除 rumble 狀態並結束此 scope。
    /// </summary>
    public void Dispose()
    {
        Action? clear = _clearRumbleState;
        if (clear is null)
        {
            return;
        }

        _clearRumbleState = null;
        clear();
        GC.SuppressFinalize(this);
    }
}
