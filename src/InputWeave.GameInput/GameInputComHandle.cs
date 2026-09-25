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
    /// <typeparam name="T">The vtable wrapper struct type. vtable 包裝結構型別。</typeparam>
    /// <param name="create">Creates the vtable wrapper from the raw pointer. 由原始指標建立 vtable 包裝。</param>
    /// <param name="objectName">The object name reported when the handle is already closed. handle 已關閉時回報的物件名稱。</param>
    /// <returns>The lease; dispose it when the native call returns. 租約；原生呼叫返回後必須釋放。</returns>
    public ComLease<T> Acquire<T>(Func<IntPtr, T> create, string objectName)
        where T : struct
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

        return new ComLease<T>(this, create(handle));
    }

    /// <inheritdoc />
    protected override bool ReleaseHandle()
    {
        Marshal.Release(handle);
        return true;
    }
}

/// <summary>
/// A lease on a <see cref="GameInputComHandle"/> that keeps the native object alive until disposed.
/// <see cref="GameInputComHandle"/> 的租約，在釋放前讓原生物件保持存活。
/// </summary>
/// <typeparam name="T">The vtable wrapper struct type. vtable 包裝結構型別。</typeparam>
/// <param name="handle">The leased handle. 被租用的 handle。</param>
/// <param name="native">The vtable wrapper for the leased pointer. 租用指標的 vtable 包裝。</param>
internal readonly ref struct ComLease<T>(GameInputComHandle handle, T native)
    where T : struct
{
    /// <summary>
    /// The vtable wrapper that is valid while the lease is held.
    /// 持有租約期間有效的 vtable 包裝。
    /// </summary>
    public T Native { get; } = native;

    /// <summary>
    /// Ends the lease.
    /// 結束租約。
    /// </summary>
    public void Dispose()
    {
        handle.DangerousRelease();
    }
}
