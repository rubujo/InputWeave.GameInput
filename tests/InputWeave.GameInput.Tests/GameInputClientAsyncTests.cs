using InputWeave.GameInput.Interop;

using static InputWeave.GameInput.Tests.TestSupport;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputClientAsyncTests
{
    [TestMethod]
    public async Task EnumerateDevicesAsyncMatchesEnumerateDevices()
    {
        await RunWithClientAsync(async client =>
        {
            IReadOnlyList<GameInputDevice> expected = client.EnumerateDevices(GameInputKind.GameInputKindGamepad);
            IReadOnlyList<GameInputDevice> actual = await client.EnumerateDevicesAsync(GameInputKind.GameInputKindGamepad);

            try
            {
                Assert.AreEqual(expected.Count, actual.Count);
            }
            finally
            {
                foreach (GameInputDevice device in expected)
                {
                    device.Dispose();
                }

                foreach (GameInputDevice device in actual)
                {
                    device.Dispose();
                }
            }
        });
    }

    [TestMethod]
    public async Task WaitForGamepadAsyncCancelsWithoutNativeReading()
    {
        await RunWithClientAsync(async client =>
        {
            using CancellationTokenSource cancellationSource = new();
            Task<GamepadReadingSnapshot?> waitTask = client.WaitForGamepadAsync(cancellationToken: cancellationSource.Token);
            cancellationSource.Cancel();

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => waitTask);
        });
    }

    [TestMethod]
    public async Task WaitForGamepadAsyncCompletesWithObjectDisposedExceptionWhenClientDisposedWhilePending()
    {
        await RunWithClientAsync(async client =>
        {
            Task<GamepadReadingSnapshot?> waitTask = client.WaitForGamepadAsync();
            client.Dispose();

            ObjectDisposedException exception = await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => waitTask);
            Assert.AreEqual(nameof(GameInputClient), exception.ObjectName);
        });
    }

    [TestMethod]
    public void DisposeFromWithinNativeCallbackThreadDoesNotThrowAndReleasesRegistrations()
    {
        RunWithClient(client =>
        {
            GameInputCallbackRegistration registration = client.RegisterDeviceCallback(
                null,
                GameInputKind.GameInputKindGamepad,
                GameInputDeviceStatus.GameInputDeviceAnyStatus,
                GameInputEnumerationKind.GameInputAsyncEnumeration,
                static (_, _, _, _) => { });

            GameInputCallbackThread.Enter();
            try
            {
                client.Dispose();
            }
            finally
            {
                GameInputCallbackThread.Exit();
            }

            Assert.IsTrue(
                SpinWait.SpinUntil(() => registration.IsDisposed, TimeSpan.FromSeconds(2)),
                "在原生回呼執行緒中呼叫 Dispose() 時，registration 應該透過背景執行緒安全釋放，而不是永遠不釋放。");
        });
    }

    [TestMethod]
    public void FindDeviceFromPlatformStringThrowsArgumentExceptionWhenValueExceedsMaxLength()
    {
        RunWithClient(client =>
        {
            string tooLong = new('a', GameInputClient.MaxPlatformStringLength + 1);

            ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => client.FindDeviceFromPlatformString(tooLong));
            Assert.AreEqual("value", exception.ParamName);
        });
    }
}
