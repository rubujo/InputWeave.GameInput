namespace InputWeave.GameInput.Tests;

/// <summary>
/// 多個測試類別共用的輔助方法。
/// </summary>
internal static class TestSupport
{
    /// <summary>
    /// 以專用執行緒同時起跑多份 <paramref name="action"/>，盡量放大並行競爭。
    /// </summary>
    public static void RunConcurrently(int threadCount, Action action)
    {
        using Barrier barrier = new(threadCount);
        Task[] tasks = [.. Enumerable.Range(0, threadCount).Select(_ => Task.Factory.StartNew(
            () =>
            {
                barrier.SignalAndWait();
                action();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default))];
        Task.WaitAll(tasks);
    }

    /// <summary>
    /// 建立 <see cref="GameInputClient"/> 執行 <paramref name="action"/>；沒有 GameInput 執行階段時標示為 Inconclusive。
    /// </summary>
    public static void RunWithClient(Action<GameInputClient> action)
    {
        try
        {
            using GameInputClient client = GameInputClient.Create();
            action(client);
        }
        catch (DllNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境未載入 GameInput.dll：{ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境的 GameInput.dll 不含必要進入點：{ex.Message}");
        }
    }

    /// <summary>
    /// <see cref="RunWithClient"/> 的非同步版本。
    /// </summary>
    public static async Task RunWithClientAsync(Func<GameInputClient, Task> action)
    {
        try
        {
            using GameInputClient client = GameInputClient.Create();
            await action(client);
        }
        catch (DllNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境未載入 GameInput.dll：{ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境的 GameInput.dll 不含必要進入點：{ex.Message}");
        }
    }
}
