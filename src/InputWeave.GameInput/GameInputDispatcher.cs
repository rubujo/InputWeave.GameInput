using System;
using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
    /// <summary>
    /// GameInput callback dispatcher 包裝。
    /// </summary>
    public sealed class GameInputDispatcher : IDisposable
    {
        private IGameInputDispatcher? _native;
        private bool _disposed;

        internal GameInputDispatcher(IGameInputDispatcher native)
        {
            _native = native;
        }

        /// <summary>
        /// 依指定 quota dispatch callback。
        /// </summary>
        /// <param name="quotaInMicroseconds">可用時間，單位為微秒。</param>
        /// <returns>若還有工作待處理，傳回 <see langword="true"/>。</returns>
        public bool Dispatch(ulong quotaInMicroseconds)
        {
            return Native.Dispatch(quotaInMicroseconds);
        }

        /// <summary>
        /// 開啟 dispatcher wait handle。
        /// </summary>
        /// <returns>GameInput 傳回的 native wait handle。</returns>
        public IntPtr OpenWaitHandle()
        {
            int hResult = Native.OpenWaitHandle(out IntPtr waitHandle);
            GameInputException.ThrowIfFailed(hResult);
            return waitHandle;
        }

        /// <summary>
        /// 開啟 dispatcher wait handle，並由 managed wrapper 負責關閉 handle。
        /// </summary>
        public GameInputDispatcherWaitHandle OpenSafeWaitHandle()
        {
            IntPtr waitHandle = OpenWaitHandle();
            return new GameInputDispatcherWaitHandle(waitHandle);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_native is not null)
            {
                Marshal.ReleaseComObject(_native);
                _native = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private IGameInputDispatcher Native
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(GameInputDispatcher));
                }

                return _native ?? throw new ObjectDisposedException(nameof(GameInputDispatcher));
            }
        }
    }
}
