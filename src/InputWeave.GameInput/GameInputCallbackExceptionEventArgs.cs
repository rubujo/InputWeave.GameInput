namespace InputWeave.GameInput;

/// <summary>
/// The event arguments for <see cref="GameInputClient.UnhandledCallbackException"/>, carrying the exception intercepted in a
/// native callback.
/// <see cref="GameInputClient.UnhandledCallbackException"/> 事件引數，攜帶原生回呼中被攔截的例外。
/// </summary>
/// <param name="exception">The exception thrown by the user delegate and intercepted. 使用者委派拋出且被攔截的例外。</param>
public sealed class GameInputCallbackExceptionEventArgs(Exception exception) : EventArgs
{
    /// <summary>
    /// The exception thrown by the user delegate and intercepted.
    /// 使用者委派拋出且被攔截的例外。
    /// </summary>
    public Exception Exception { get; } = exception;
}
