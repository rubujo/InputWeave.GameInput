using System.Runtime.InteropServices;

namespace InputWeave.GameInput;

/// <summary>
/// Owns one COM reference to a GameInput object; <see cref="SafeHandle"/> guarantees the reference is released exactly once,
/// either by <see cref="SafeHandle.Dispose()"/> or, when the owner is never disposed, by the critical finalizer, and only after
/// every outstanding <see cref="SafeHandle.DangerousAddRef"/> lease has ended.
/// 擁有 GameInput 物件的一份 COM 參考；<see cref="SafeHandle"/> 保證這份參考只釋放一次——由
/// <see cref="SafeHandle.Dispose()"/> 釋放，或在擁有者從未釋放時由關鍵終結器釋放——且只在所有進行中的
/// <see cref="SafeHandle.DangerousAddRef"/> 租約結束後才釋放。
/// </summary>
internal sealed class GameInputComHandle : SafeHandle
{
    /// <summary>
    /// Takes ownership of a COM reference that the caller already holds.
    /// 接手呼叫端已持有的一份 COM 參考。
    /// </summary>
    /// <param name="pointer">The owned COM pointer. 已擁有的 COM 指標。</param>
    public GameInputComHandle(IntPtr pointer)
        : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(pointer);
    }

    /// <inheritdoc />
    public override bool IsInvalid
    {
        get
        {
            return handle == IntPtr.Zero;
        }
    }

    /// <summary>
    /// Acquires a lease that keeps the COM object alive for the duration of one native call, even if the owner is disposed or
    /// becomes unreachable in the meantime.
    /// 取得在一次原生呼叫期間讓 COM 物件保持存活的租約；即使期間擁有者被釋放或變成無法存取也一樣。
    /// </summary>
    /// <typeparam name="T">The vtable wrapper struct type, which holds only the COM pointer. 只持有 COM 指標的 vtable 包裝結構型別。</typeparam>
    /// <param name="objectName">The object name reported when the handle is already closed. handle 已關閉時回報的物件名稱。</param>
    /// <returns>The lease; dispose it when the native call returns. 租約；原生呼叫返回後必須釋放。</returns>
    public ComLease<T> Acquire<T>(string objectName)
        where T : unmanaged
    {
        bool success = false;
        try
        {
            DangerousAddRef(ref success);
        }
        catch (ObjectDisposedException)
        {
            throw new ObjectDisposedException(objectName);
        }

        return new ComLease<T>(this, FromPointer<T>(handle));
    }

    /// <summary>
    /// Reinterprets a COM pointer as a vtable wrapper struct without a factory delegate.
    /// 不經工廠委派，直接把 COM 指標重新解讀為 vtable 包裝結構。
    /// </summary>
    /// <typeparam name="T">The vtable wrapper struct type, which holds only the COM pointer. 只持有 COM 指標的 vtable 包裝結構型別。</typeparam>
    /// <param name="pointer">The COM pointer. COM 指標。</param>
    /// <returns>The vtable wrapper. vtable 包裝。</returns>
    public static unsafe T FromPointer<T>(IntPtr pointer)
        where T : unmanaged
    {
        // 產生式 vtable 包裝結構只有一個 IntPtr 欄位；大小不符代表產生器輸出改變，必須立即失敗而非讀錯記憶體。
        return sizeof(T) == sizeof(IntPtr)
            ? *(T*)&pointer
            : throw new InvalidOperationException($"{typeof(T).Name} 不是只包含 COM 指標的 vtable 包裝結構。");
    }

    /// <inheritdoc />
    protected override bool ReleaseHandle()
    {
        Marshal.Release(handle);
        return true;
    }
}

/// <summary>
/// A lease on a <see cref="GameInputComHandle"/> that keeps the native object alive until disposed. The default value is an
/// empty lease used for optional native arguments.
/// <see cref="GameInputComHandle"/> 的租約，在釋放前讓原生物件保持存活。預設值是空租約，用於選用的原生參數。
/// </summary>
/// <typeparam name="T">The vtable wrapper struct type. vtable 包裝結構型別。</typeparam>
internal readonly ref struct ComLease<T>
    where T : unmanaged
{
    private readonly GameInputComHandle? _handle;

    /// <summary>
    /// Creates a lease; <paramref name="handle"/> is <see langword="null"/> when the caller already guarantees the lifetime.
    /// 建立租約；呼叫端已保證生命週期時 <paramref name="handle"/> 為 <see langword="null"/>。
    /// </summary>
    /// <param name="handle">The leased handle, whose reference is released on dispose. 被租用的 handle，釋放時歸還參考。</param>
    /// <param name="native">The vtable wrapper for the leased pointer. 租用指標的 vtable 包裝。</param>
    public ComLease(GameInputComHandle? handle, T native)
    {
        _handle = handle;
        Native = native;
        HasValue = true;
    }

    /// <summary>
    /// The vtable wrapper that is valid while the lease is held.
    /// 持有租約期間有效的 vtable 包裝。
    /// </summary>
    public T Native { get; }

    /// <summary>
    /// Whether the lease refers to a native object; <see langword="false"/> for the empty lease.
    /// 租約是否指向原生物件；空租約為 <see langword="false"/>。
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// The vtable wrapper, or <see langword="null"/> for the empty lease, suitable for optional native arguments.
    /// vtable 包裝；空租約時為 <see langword="null"/>，適用於選用的原生參數。
    /// </summary>
    public T? NativeOrNull
    {
        get
        {
            return HasValue ? Native : null;
        }
    }

    /// <summary>
    /// Ends the lease.
    /// 結束租約。
    /// </summary>
    public void Dispose()
    {
        _handle?.DangerousRelease();
    }
}
