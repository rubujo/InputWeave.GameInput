using Microsoft.Extensions.DependencyInjection;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddGameInputDeviceManagerResolvesSingletonInstance()
    {
        ServiceCollection services = new();
        services.AddGameInputDeviceManager();

        try
        {
            using ServiceProvider provider = services.BuildServiceProvider();
            GameInputDeviceManager first = provider.GetRequiredService<GameInputDeviceManager>();
            GameInputDeviceManager second = provider.GetRequiredService<GameInputDeviceManager>();

            Assert.AreSame(first, second);
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

    [TestMethod]
    public void AddGameInputClientResolvesSingletonInstance()
    {
        ServiceCollection services = new();
        services.AddGameInputClient();

        try
        {
            using ServiceProvider provider = services.BuildServiceProvider();
            GameInputClient first = provider.GetRequiredService<GameInputClient>();
            GameInputClient second = provider.GetRequiredService<GameInputClient>();

            Assert.AreSame(first, second);
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
