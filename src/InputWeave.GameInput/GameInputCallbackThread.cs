namespace InputWeave.GameInput;

internal static class GameInputCallbackThread
{
    [ThreadStatic]
    private static bool t_isExecutingCallback;

    internal static bool IsExecutingCallback
    {
        get { return t_isExecutingCallback; }
    }

    internal static void Enter()
    {
        t_isExecutingCallback = true;
    }

    internal static void Exit()
    {
        t_isExecutingCallback = false;
    }
}
