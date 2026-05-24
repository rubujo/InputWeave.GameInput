using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

/// <summary>
/// GameInput 原生回呼註冊。
/// </summary>
public sealed class GameInputCallbackRegistration : IDisposable
{
    private readonly Action<ulong> _stopCallback;
    private readonly Func<ulong, bool> _unregisterCallback;
    private readonly Action<GameInputCallbackRegistration> _removeRegistration;
    private readonly Action _deactivateContext;
    private GCHandle _contextHandle;

    internal GameInputCallbackRegistration(
        ulong token,
        GCHandle contextHandle,
        Action deactivateContext,
        Action<ulong> stopCallback,
        Func<ulong, bool> unregisterCallback,
        Action<GameInputCallbackRegistration> removeRegistration)
    {
        Token = token;
        _contextHandle = contextHandle;
        _deactivateContext = deactivateContext;
        _stopCallback = stopCallback;
        _unregisterCallback = unregisterCallback;
        _removeRegistration = removeRegistration;
    }

    /// <summary>
    /// 原生 GameInput callback token。
    /// </summary>
    public ulong Token { get; }

    /// <summary>
    /// 註冊是否已釋放。
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// 取消註冊 callback 並釋放相關 managed 狀態。
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        _deactivateContext();

        try
        {
            if (Token != 0)
            {
                _stopCallback(Token);
                _unregisterCallback(Token);
            }
        }
        finally
        {
            if (_contextHandle.IsAllocated)
            {
                _contextHandle.Free();
            }

            _removeRegistration(this);
            GC.SuppressFinalize(this);
        }
    }
}
