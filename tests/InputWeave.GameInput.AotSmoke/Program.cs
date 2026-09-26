using System.Runtime.CompilerServices;
using InputWeave.GameInput;
using InputWeave.GameInput.Interop;
using Microsoft.Extensions.DependencyInjection;

// NativeAOT 煙霧測試：由 CI 以 dotnet publish -p:PublishAot=true 發佈後直接執行原生執行檔。
// 沒有 GameInput 執行階段的環境（例如 CI 代理程式）只驗證受控載入器與探測路徑；有執行階段時再走主要高階 API。
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine($"執行階段：{Environment.Version}，NativeAOT：{!RuntimeFeature.IsDynamicCodeSupported}");

if (RuntimeFeature.IsDynamicCodeSupported && !string.Equals(Environment.GetEnvironmentVariable("INPUTWEAVE_AOT_SMOKE_ALLOW_JIT"), "1", StringComparison.Ordinal))
{
    Console.Error.WriteLine("此程式應以 NativeAOT 發佈後執行；如需以 JIT 偵錯，請設定 INPUTWEAVE_AOT_SMOKE_ALLOW_JIT=1。");
    return 2;
}

bool available = GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo probe);
Console.WriteLine($"GameInput 探測：可用={available}，載入原則={probe.LoaderPolicy}，HRESULT=0x{probe.HResult:X8}");
if (!available)
{
    Console.WriteLine("此環境沒有 GameInput 執行階段，只驗證載入器與探測路徑。");
    return 0;
}

const GameInputKind kinds = GameInputKind.GameInputKindGamepad | GameInputKind.GameInputKindKeyboard | GameInputKind.GameInputKindMouse;

using (GameInputClient client = GameInputClient.Create())
{
    IReadOnlyList<GameInputDevice> devices = await client.EnumerateDevicesAsync(kinds);
    Console.WriteLine($"列舉到 {devices.Count} 個裝置");
    foreach (GameInputDevice device in devices)
    {
        using (device)
        {
            GameInputDeviceInfoSnapshot info = device.GetDeviceInfoSnapshot();
            if (!info.Equals(device.GetDeviceInfoSnapshot()) || info.GetHashCode() != device.GetDeviceInfoSnapshot().GetHashCode())
            {
                Console.Error.WriteLine($"裝置快照相等性不一致：{info.DisplayName}");
                return 1;
            }

            Console.WriteLine($"  {info.DisplayName}：{info.SupportedInput}，VID 0x{info.VendorId:X4} / PID 0x{info.ProductId:X4}");
        }
    }

    _ = client.TryGetCurrentGamepad(out GamepadReadingSnapshot gamepad);
    Console.WriteLine($"目前 Gamepad 按鈕：{gamepad.State.Buttons}");
}

ServiceCollection services = new();
services.AddGameInputDeviceManager();
using (ServiceProvider provider = services.BuildServiceProvider())
{
    GameInputDeviceManager manager = provider.GetRequiredService<GameInputDeviceManager>();
    manager.RefreshDevices();
    EventHandler<GameInputDeviceManagerEvent> handler = static (_, _) => { };
    manager.DeviceChanged += handler;
    manager.DeviceChanged -= handler;
    Console.WriteLine($"依賴注入解析的 manager 快取 {manager.DeviceSnapshots.Count} 個裝置快照");
}

Console.WriteLine("NativeAOT 煙霧測試通過。");
return 0;
