namespace InputWeave.GameInput;

/// <summary>
/// <see cref="GameInputClient.UnhandledCallbackException"/> 事件引數，攜帶原生回呼中被攔截的例外。
/// </summary>
/// <param name="exception">使用者委派拋出且被攔截的例外。</param>
public sealed class GameInputCallbackExceptionEventArgs(Exception exception) : EventArgs
{
    /// <summary>
    /// 使用者委派拋出且被攔截的例外。
    /// </summary>
    public Exception Exception { get; } = exception;
}
