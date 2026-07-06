using Microsoft.Extensions.DependencyInjection;

namespace InputWeave.GameInput;

/// <summary>
/// 提供 <see cref="IServiceCollection"/> 的 GameInput 依賴注入註冊擴充方法。
/// </summary>
public static class GameInputServiceCollectionExtensions
{
    /// <summary>
    /// 以單例（Singleton）方式註冊 <see cref="GameInputClient"/>，容器釋放時會一併釋放其持有的 GameInput 資源。
    /// </summary>
    /// <param name="services">要註冊的服務集合。</param>
    /// <returns>傳入的服務集合，便於串接呼叫。</returns>
    public static IServiceCollection AddGameInputClient(this IServiceCollection services)
    {
        return services.AddSingleton(static _ => GameInputClient.Create());
    }

    /// <summary>
    /// 以單例（Singleton）方式註冊 <see cref="GameInputDeviceManager"/>，容器釋放時會一併釋放其持有的 GameInput 資源。
    /// </summary>
    /// <param name="services">要註冊的服務集合。</param>
    /// <returns>傳入的服務集合，便於串接呼叫。</returns>
    public static IServiceCollection AddGameInputDeviceManager(this IServiceCollection services)
    {
        return services.AddSingleton(static _ => GameInputDeviceManager.Create());
    }
}
