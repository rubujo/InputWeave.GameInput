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
    /// <remarks>
    /// <see cref="GameInputDeviceManager.Create()"/> 會在內部自建獨立的 <see cref="GameInputClient"/>；同時呼叫這個方法與
    /// <see cref="AddGameInputDeviceManager"/> 不會共用同一個 <see cref="GameInputClient"/> 執行個體，而是分別建立兩個互不相干的
    /// GameInput 用戶端。一般情境應該二擇一註冊，不要同時使用兩者。
    /// </remarks>
    /// <param name="services">要註冊的服務集合。</param>
    /// <returns>傳入的服務集合，便於串接呼叫。</returns>
    public static IServiceCollection AddGameInputClient(this IServiceCollection services)
    {
        return services.AddSingleton(static _ => GameInputClient.Create());
    }

    /// <summary>
    /// 以單例（Singleton）方式註冊 <see cref="GameInputDeviceManager"/>，容器釋放時會一併釋放其持有的 GameInput 資源。
    /// </summary>
    /// <remarks>
    /// 這個方法會透過 <see cref="GameInputDeviceManager.Create()"/> 建立內部專屬的 <see cref="GameInputClient"/>；同時呼叫
    /// <see cref="AddGameInputClient"/> 不會讓兩者共用同一個 GameInput 用戶端，而是分別建立兩個互不相干的執行個體。一般情境應該
    /// 二擇一註冊，不要同時使用兩者。
    /// </remarks>
    /// <param name="services">要註冊的服務集合。</param>
    /// <returns>傳入的服務集合，便於串接呼叫。</returns>
    public static IServiceCollection AddGameInputDeviceManager(this IServiceCollection services)
    {
        return services.AddSingleton(static _ => GameInputDeviceManager.Create());
    }
}
