using System;
using System.ComponentModel;
using System.Globalization;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput
{
    /// <summary>
    /// GameInput 原生呼叫失敗時擲出的例外狀況。
    /// </summary>
    public sealed class GameInputException : Win32Exception
    {
        /// <summary>
        /// 使用 HRESULT 建立 GameInput 例外狀況。
        /// </summary>
        /// <param name="hResult">GameInput 傳回的 HRESULT。</param>
        public GameInputException(int hResult)
            : base(hResult, CreateMessage(hResult))
        {
            HResult = hResult;
        }

        /// <summary>
        /// HRESULT 是否代表 GameInput 裝置或讀取資料不存在。
        /// </summary>
        public bool IsNotFound
        {
            get
            {
                return HResult == GameInputHResult.DeviceNotFound
                    || HResult == GameInputHResult.ReadingNotFound
                    || HResult == GameInputHResult.InputKindNotPresent;
            }
        }

        internal static void ThrowIfFailed(int hResult)
        {
            if (hResult < 0)
            {
                throw new GameInputException(hResult);
            }
        }

        private static string CreateMessage(int hResult)
        {
            return hResult switch
            {
                GameInputHResult.DeviceDisconnected => "GameInput 裝置已中斷連線。",
                GameInputHResult.DeviceNotFound => "找不到指定的 GameInput 裝置。",
                GameInputHResult.ReadingNotFound => "找不到符合條件的 GameInput 讀取資料。",
                GameInputHResult.ReferenceReadingTooOld => "GameInput 參考讀取資料太舊。",
                GameInputHResult.FeedbackNotSupported => "GameInput 裝置不支援指定的力回饋功能。",
                GameInputHResult.ObjectNoLongerExists => "GameInput 原生物件已不再存在。",
                GameInputHResult.CallbackNotFound => "找不到指定的 GameInput 回呼。",
                GameInputHResult.HapticInfoNotFound => "找不到 GameInput 觸覺資訊。",
                GameInputHResult.AggregateOperationNotSupported => "GameInput 不支援指定的聚合裝置操作。",
                GameInputHResult.InputKindNotPresent => "裝置沒有指定的 GameInput 輸入種類。",
                _ => string.Format(CultureInfo.InvariantCulture, "GameInput 呼叫失敗，HRESULT: 0x{0:X8}。", hResult)
            };
        }
    }
}
