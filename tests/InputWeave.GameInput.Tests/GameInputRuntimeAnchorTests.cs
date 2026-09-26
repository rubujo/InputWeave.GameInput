namespace InputWeave.GameInput.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GameInputRuntimeAnchorTests
{
    [TestMethod]
    public void ConcurrentClientCreateAndDisposeDoesNotCrashRuntime()
    {
        // 測試期間不得持有任何存活的 client，否則它本身就會讓單例根物件的參考計數不歸零，無法重現原本的崩潰。
        if (!GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo probe))
        {
            Assert.Inconclusive($"此測試環境沒有可用的 GameInput 執行階段（HRESULT 0x{probe.HResult:X8}）。");
        }

        // 不保留單例根物件的錨點參考時，這個重疊情境會讓 GameInput 執行階段以存取違規結束整個處理序（實測必定重現）。
        bool stop = false;
        Thread churn = new(() =>
        {
            while (!Volatile.Read(ref stop))
            {
                GameInputClient.Create().Dispose();
            }
        });
        churn.Start();
        try
        {
            DateTime end = DateTime.UtcNow.AddSeconds(3);
            while (DateTime.UtcNow < end)
            {
                using GameInputClient client = GameInputClient.Create();
                _ = client.TryGetCurrentKeyboard(out _);
            }
        }
        finally
        {
            Volatile.Write(ref stop, true);
            churn.Join();
        }
    }
}
