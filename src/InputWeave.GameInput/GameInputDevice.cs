using System.Runtime.InteropServices;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput;

/// <summary>
/// A GameInput device wrapper.
/// GameInput 裝置包裝。
/// </summary>
public sealed class GameInputDevice : IDisposable
{
    private IGameInputDevice? _native;
    private bool _disposed;
    private GameInputDeviceInfoSnapshot? _cachedInfoSnapshot;

#if NET10_0_OR_GREATER
    private readonly System.Threading.Lock _cacheSyncRoot = new();
#else
    private readonly object _cacheSyncRoot = new();
#endif

    internal GameInputDevice(IGameInputDevice native)
    {
        _native = native;
    }

    internal IGameInputDevice NativeInterface
    {
        get
        {
            return Native;
        }
    }

    /// <summary>
    /// The current device status.
    /// 目前裝置狀態。
    /// </summary>
    public GameInputDeviceStatus Status
    {
        get
        {
            return Native.GetDeviceStatus();
        }
    }

    /// <summary>
    /// Gets the device information.
    /// 取得裝置資訊。
    /// </summary>
    /// <remarks>
    /// The string and array fields inside the returned structure (such as <see cref="GameInputDeviceInfo.DisplayName"/> and
    /// <see cref="GameInputDeviceInfo.PnpPath"/>) are pointers into native memory whose lifetime is bound to the underlying COM
    /// object of this <see cref="GameInputDevice"/>. Accessing those pointers after the device is disposed causes undefined
    /// behavior (for example <see cref="AccessViolationException"/>). In typical scenarios use
    /// <see cref="GetDeviceInfoSnapshot"/>, which holds no native pointers.
    /// 傳回結構內的字串與陣列欄位（例如 <see cref="GameInputDeviceInfo.DisplayName"/>、
    /// <see cref="GameInputDeviceInfo.PnpPath"/>）是指向原生記憶體的指標，其生命週期綁定於這個
    /// <see cref="GameInputDevice"/> 的底層 COM 物件。裝置被 <see cref="Dispose"/> 後再存取這些指標會導致未定義行為
    /// （例如 <see cref="AccessViolationException"/>）。一般情境應改用不持有原生指標的
    /// <see cref="GetDeviceInfoSnapshot"/>。
    /// </remarks>
    /// <returns>The native device information structure. 原生裝置資訊結構。</returns>
    public GameInputDeviceInfo GetDeviceInfo()
    {
        int hResult = Native.GetDeviceInfo(out IntPtr info);
        GameInputException.ThrowIfFailed(hResult);
        return Marshal.PtrToStructure<GameInputDeviceInfo>(info);
    }

    /// <summary>
    /// Gets the device display name.
    /// 取得裝置顯示名稱。
    /// </summary>
    /// <returns>The device display name, or null when unavailable. 裝置顯示名稱；無法取得時為 null。</returns>
    /// <exception cref="InvalidOperationException">The string length reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的字串長度超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public string? GetDisplayName()
    {
        GameInputDeviceInfo info = GetDeviceInfo();
        return NativeUtf8String.FromNullTerminated(info.DisplayName);
    }

    /// <summary>
    /// Gets the device PnP path.
    /// 取得裝置 PnP 路徑。
    /// </summary>
    /// <returns>The device PnP path, or null when unavailable. 裝置 PnP 路徑；無法取得時為 null。</returns>
    /// <exception cref="InvalidOperationException">The string length reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的字串長度超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public string? GetPnpPath()
    {
        GameInputDeviceInfo info = GetDeviceInfo();
        return NativeUtf8String.FromNullTerminated(info.PnpPath);
    }

    /// <summary>
    /// Gets a device information snapshot that holds no native pointers.
    /// 取得不持有原生指標的裝置資訊快照。
    /// </summary>
    /// <remarks>
    /// Device capabilities (such as supported input kinds and rumble motors) are fixed for the lifetime of the device, so this
    /// method caches the result on the same <see cref="GameInputDevice"/> instance to avoid repeated full native marshaling.
    /// 裝置能力（例如支援的輸入種類、rumble 馬達）在裝置存續期間為固定值，此方法會在同一個
    /// <see cref="GameInputDevice"/> 執行個體上快取結果，避免重複的完整原生 marshal。
    /// </remarks>
    /// <returns>The device information snapshot that holds no native pointers. 不持有原生指標的裝置資訊快照。</returns>
    public GameInputDeviceInfoSnapshot GetDeviceInfoSnapshot()
    {
        if (_cachedInfoSnapshot is { } cached)
        {
            return cached;
        }

        lock (_cacheSyncRoot)
        {
            return _cachedInfoSnapshot ??= GameInputDeviceInfoSnapshot.FromNative(GetDeviceInfo());
        }
    }

    /// <summary>
    /// Tries to get the haptic information.
    /// 嘗試取得觸覺資訊。
    /// </summary>
    /// <returns>The haptic information, or null when the device provides none. 觸覺資訊；裝置未提供時為 null。</returns>
    public GameInputHapticInfo? GetHapticInfo()
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputHapticInfo>());
        try
        {
            int hResult = Native.GetHapticInfo(pointer);
            if (hResult == GameInputHResult.HapticInfoNotFound)
            {
                return null;
            }

            GameInputException.ThrowIfFailed(hResult);
            return Marshal.PtrToStructure<GameInputHapticInfo>(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// Tries to get a haptic information snapshot that holds no native buffers.
    /// 嘗試取得不持有原生緩衝區的觸覺資訊快照。
    /// </summary>
    /// <returns>The haptic information snapshot, or null when the device provides none. 觸覺資訊快照；裝置未提供時為 null。</returns>
    public GameInputHapticInfoSnapshot? GetHapticInfoSnapshot()
    {
        GameInputHapticInfo? hapticInfo = GetHapticInfo();
        return hapticInfo.HasValue ? GameInputHapticInfoSnapshot.FromNative(hapticInfo.GetValueOrDefault()) : null;
    }

    /// <summary>
    /// Tries to get a haptic information snapshot that holds no native buffers.
    /// 嘗試取得不持有原生緩衝區的觸覺資訊快照。
    /// </summary>
    /// <param name="snapshot">Receives the haptic information snapshot on success. 成功時接收觸覺資訊快照。</param>
    /// <returns>Returns true when the device provides haptic information; otherwise returns false. 若裝置提供觸覺資訊，傳回 true；否則傳回 false。</returns>
    public bool TryGetHapticInfoSnapshot(out GameInputHapticInfoSnapshot? snapshot)
    {
        snapshot = GetHapticInfoSnapshot();
        return snapshot is not null;
    }

    /// <summary>
    /// Creates a force feedback effect.
    /// 建立 force feedback effect。
    /// </summary>
    /// <param name="motorIndex">The force feedback motor index. force feedback motor 索引。</param>
    /// <param name="parameters">The native GameInput parameters. GameInput 原生參數。</param>
    /// <returns>The newly created force feedback effect. 新建立的 force feedback effect。</returns>
    public GameInputForceFeedbackEffect CreateForceFeedbackEffect(uint motorIndex, in GameInputForceFeedbackParams parameters)
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputForceFeedbackParams>());
        try
        {
            Marshal.StructureToPtr(parameters, pointer, fDeleteOld: false);
            int hResult = Native.CreateForceFeedbackEffect(motorIndex, pointer, out IGameInputForceFeedbackEffect? effect);
            GameInputException.ThrowIfFailed(hResult);
            return effect is { } effectValue
                ? new GameInputForceFeedbackEffect(effectValue)
                : throw new GameInputException(GameInputHResult.FeedbackNotSupported);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// Tries to create a force feedback effect without throwing when unsupported.
    /// 嘗試建立 force feedback effect；不支援時不擲出例外。
    /// </summary>
    /// <param name="motorIndex">The force feedback motor index. force feedback motor 索引。</param>
    /// <param name="parameters">The native GameInput parameters. GameInput 原生參數。</param>
    /// <param name="effect">Receives the force feedback effect on success. 成功時接收 force feedback effect。</param>
    /// <returns>Returns true when the device supports the effect and it was created successfully; otherwise returns false. 若裝置支援並成功建立 effect，傳回 true；否則傳回 false。</returns>
    public bool TryCreateForceFeedbackEffect(uint motorIndex, in GameInputForceFeedbackParams parameters, out GameInputForceFeedbackEffect? effect)
    {
        if (!IsForceFeedbackEffectSupported(motorIndex, parameters.Kind))
        {
            effect = null;
            return false;
        }

        try
        {
            effect = CreateForceFeedbackEffect(motorIndex, in parameters);
            return true;
        }
        catch (GameInputException ex) when (ex.HResult is GameInputHResult.FeedbackNotSupported
            or GameInputHResult.DeviceDisconnected
            or GameInputHResult.DeviceNotFound
            or GameInputHResult.ObjectNoLongerExists)
        {
            // 裝置可能在能力檢查與建立 effect 之間被拔除；依 Try 方法契約歸類為「不支援」而非向外拋出。
            effect = null;
            return false;
        }
    }

    /// <summary>
    /// Checks whether the specified force feedback motor supports the specified effect kind.
    /// 檢查指定 force feedback motor 是否支援指定 effect 類型。
    /// </summary>
    /// <param name="motorIndex">The force feedback motor index. force feedback motor 索引。</param>
    /// <param name="effectKind">The force feedback effect kind to check. 要檢查的 force feedback effect 類型。</param>
    /// <returns>若裝置宣告支援指定 effect，傳回 true；否則傳回 false。原生回報的裝置資訊超過內部上限時
    /// 也視為不支援，傳回 false 而非拋出例外。</returns>
    public bool IsForceFeedbackEffectSupported(uint motorIndex, GameInputForceFeedbackEffectKind effectKind)
    {
        GameInputDeviceInfoSnapshot snapshot;
        try
        {
            snapshot = GetDeviceInfoSnapshot();
        }
        catch (InvalidOperationException ex) when (ex is not ObjectDisposedException)
        {
            // 原生回報的裝置資訊（字串長度或陣列數量）超過 NativeSizeGuard 上限，
            // 依能力查詢語意歸類為「不支援」；ObjectDisposedException 屬於呼叫端程式錯誤，仍往外拋。
            return false;
        }

        if (motorIndex >= snapshot.ForceFeedbackMotors.Count)
        {
            return false;
        }

        GameInputForceFeedbackMotorInfo motor = snapshot.ForceFeedbackMotors[checked((int)motorIndex)];
        return IsForceFeedbackEffectSupported(motor, effectKind);
    }

    /// <summary>
    /// Whether the specified motor is powered on.
    /// 指定馬達是否已供電。
    /// </summary>
    /// <param name="motorIndex">The force feedback motor index. force feedback motor 索引。</param>
    /// <returns>Returns true when the motor is powered on; otherwise returns false. 馬達已供電時傳回 true；否則傳回 false。</returns>
    public bool IsForceFeedbackMotorPoweredOn(uint motorIndex)
    {
        return Native.IsForceFeedbackMotorPoweredOn(motorIndex);
    }

    /// <summary>
    /// Sets the force feedback motor gain.
    /// 設定 force feedback 馬達 gain。
    /// </summary>
    /// <param name="motorIndex">The force feedback motor index. force feedback motor 索引。</param>
    /// <param name="masterGain">The master gain to apply. 要套用的 master gain。</param>
    public void SetForceFeedbackMotorGain(uint motorIndex, float masterGain)
    {
        Native.SetForceFeedbackMotorGain(motorIndex, masterGain);
    }

    /// <summary>
    /// Sets the rumble state.
    /// 設定震動狀態。
    /// </summary>
    /// <param name="parameters">The native GameInput parameters. GameInput 原生參數。</param>
    public void SetRumbleState(in GameInputRumbleParams parameters)
    {
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<GameInputRumbleParams>());
        try
        {
            Marshal.StructureToPtr(parameters, pointer, fDeleteOld: false);
            Native.SetRumbleState(pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    /// Tries to set the rumble state according to the device capabilities.
    /// 嘗試依裝置能力設定 rumble 狀態。
    /// </summary>
    /// <remarks>
    /// The decision depends only on whether the device declares support for any rumble motor, not on whether the requested
    /// intensities are zero; explicitly passing all-zero intensities (for example to stop rumble) therefore still returns true
    /// when the device supports rumble.
    /// 判斷式僅依裝置是否宣告支援任一 rumble motor，而非依請求強度是否為 0；因此明確傳入全部為 0
    /// 的強度（例如要停止震動）在裝置支援 rumble 時仍會傳回 true。
    /// </remarks>
    /// <param name="parameters">The GameInput rumble parameters; intensities must be between 0.0 and 1.0. GameInput rumble 參數，強度必須介於 0.0 到 1.0。</param>
    /// <returns>若裝置支援任一要求的 rumble motor 並已設定狀態，傳回 true；若裝置不支援 rumble，傳回 false。
    /// 原生回報的裝置資訊超過內部上限時也視為不支援，傳回 false 而非拋出例外。</returns>
    public bool TrySetRumble(in GameInputRumbleParams parameters)
    {
        ValidateRumbleStrength(parameters);

        GameInputRumbleMotors supportedMotors;
        try
        {
            supportedMotors = GetDeviceInfoSnapshot().SupportedRumbleMotors;
        }
        catch (InvalidOperationException ex) when (ex is not ObjectDisposedException)
        {
            // 原生回報的裝置資訊（字串長度或陣列數量）超過 NativeSizeGuard 上限，
            // 依 Try 方法契約歸類為「不支援」；ObjectDisposedException 屬於呼叫端程式錯誤，仍往外拋。
            return false;
        }

        if (supportedMotors == GameInputRumbleMotors.GameInputRumbleNone)
        {
            return false;
        }

        SetRumbleState(MaskRumble(parameters, supportedMotors));
        return true;
    }

    /// <summary>
    /// Tries to set the rumble state according to the device capabilities.
    /// 嘗試依裝置能力設定 rumble 狀態。
    /// </summary>
    /// <param name="lowFrequency">The low-frequency motor intensity; must be between 0.0 and 1.0. 低頻馬達強度，必須介於 0.0 到 1.0。</param>
    /// <param name="highFrequency">The high-frequency motor intensity; must be between 0.0 and 1.0. 高頻馬達強度，必須介於 0.0 到 1.0。</param>
    /// <param name="leftTrigger">The left trigger motor intensity; must be between 0.0 and 1.0. 左 trigger 馬達強度，必須介於 0.0 到 1.0。</param>
    /// <param name="rightTrigger">The right trigger motor intensity; must be between 0.0 and 1.0. 右 trigger 馬達強度，必須介於 0.0 到 1.0。</param>
    /// <returns>Returns true when the device supports the requested rumble motors and the state was applied; otherwise returns false. 若裝置支援要求的 rumble motor 並已設定狀態，傳回 true；否則傳回 false。</returns>
    public bool TrySetRumble(float lowFrequency, float highFrequency, float leftTrigger = 0, float rightTrigger = 0)
    {
        GameInputRumbleParams parameters = GameInputForceFeedback.Rumble(lowFrequency, highFrequency, leftTrigger, rightTrigger);
        return TrySetRumble(in parameters);
    }

    /// <summary>
    /// Tries to start a rumble scope; disposing the scope clears the rumble state.
    /// 嘗試啟動 rumble scope，釋放 scope 時會清除 rumble 狀態。
    /// </summary>
    /// <param name="parameters">The GameInput rumble parameters; intensities must be between 0.0 and 1.0. GameInput rumble 參數，強度必須介於 0.0 到 1.0。</param>
    /// <returns>Returns the scope that must be disposed when rumble was started; returns null when rumble is not supported. 若已啟動 rumble，傳回需要釋放的 scope；若不支援，傳回 null。</returns>
    public GameInputRumbleScope? StartRumbleScope(in GameInputRumbleParams parameters)
    {
        return TrySetRumble(in parameters) ? new GameInputRumbleScope(ClearRumbleState) : null;
    }

    /// <summary>
    /// Tries to start a rumble scope; disposing the scope clears the rumble state.
    /// 嘗試啟動 rumble scope，釋放 scope 時會清除 rumble 狀態。
    /// </summary>
    /// <param name="lowFrequency">The low-frequency motor intensity; must be between 0.0 and 1.0. 低頻馬達強度，必須介於 0.0 到 1.0。</param>
    /// <param name="highFrequency">The high-frequency motor intensity; must be between 0.0 and 1.0. 高頻馬達強度，必須介於 0.0 到 1.0。</param>
    /// <param name="leftTrigger">The left trigger motor intensity; must be between 0.0 and 1.0. 左 trigger 馬達強度，必須介於 0.0 到 1.0。</param>
    /// <param name="rightTrigger">The right trigger motor intensity; must be between 0.0 and 1.0. 右 trigger 馬達強度，必須介於 0.0 到 1.0。</param>
    /// <returns>Returns the scope that must be disposed when rumble was started; returns null when rumble is not supported. 若已啟動 rumble，傳回需要釋放的 scope；若不支援，傳回 null。</returns>
    public GameInputRumbleScope? StartRumbleScope(float lowFrequency, float highFrequency, float leftTrigger = 0, float rightTrigger = 0)
    {
        GameInputRumbleParams parameters = GameInputForceFeedback.Rumble(lowFrequency, highFrequency, leftTrigger, rightTrigger);
        return StartRumbleScope(in parameters);
    }

    /// <summary>
    /// Clears the rumble state.
    /// 清除震動狀態。
    /// </summary>
    /// <remarks>
    /// The native GameInput API documentation marks the <c>SetRumbleState</c> parameter as optional, allowing <c>nullptr</c> to
    /// mean stop rumbling, but testing showed that some device driver/runtime implementations trigger a native memory access
    /// violation when given a null pointer (on .NET Core / .NET 5+ such access violations are corrupted-process-state exceptions
    /// that managed exception handling cannot catch, crashing the whole process). This method therefore passes an all-zero
    /// <see cref="GameInputRumbleParams"/> instead (semantically equivalent to stopping rumble), so the native call always
    /// receives a pointer to legitimately allocated memory, avoiding this known native-side issue.
    /// GameInput 原生 API 文件把 <c>SetRumbleState</c> 的參數標為選用、可傳入 <c>nullptr</c> 表示停止震動，
    /// 但實測發現部分裝置的驅動／執行階段實作在收到空指標時會觸發原生端記憶體存取違規（這類存取違規在
    /// .NET Core / .NET 5+ 是處理序毀損狀態例外，無法用 managed 例外處理接住，會直接讓整個處理序當掉）。
    /// 因此這裡改傳入全欄位為零的 <see cref="GameInputRumbleParams"/>（語意等價於停止震動），
    /// 讓原生呼叫一律收到指向合法配置記憶體的指標，避免觸發這個已知的原生端問題。
    /// </remarks>
    public void ClearRumbleState()
    {
        SetRumbleState(default(GameInputRumbleParams));
    }

    /// <summary>
    /// Executes a DirectInput escape.
    /// 執行 DirectInput escape。
    /// </summary>
    /// <param name="command">The DirectInput escape command. DirectInput escape 命令。</param>
    /// <param name="input">The input data buffer. 輸入資料緩衝區。</param>
    /// <param name="output">The output data buffer. 輸出資料緩衝區。</param>
    /// <returns>The number of bytes actually written to the output buffer. 實際寫入輸出緩衝區的位元組數。</returns>
    public int DirectInputEscape(uint command, byte[] input, byte[] output)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
#else
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (output is null)
        {
            throw new ArgumentNullException(nameof(output));
        }
#endif

        IntPtr inputPointer = Marshal.AllocHGlobal(input.Length);
        IntPtr outputPointer = Marshal.AllocHGlobal(output.Length);
        try
        {
            Marshal.Copy(input, 0, inputPointer, input.Length);
            int hResult = Native.DirectInputEscape(command, inputPointer, (uint)input.Length, outputPointer, (uint)output.Length, out uint written);
            GameInputException.ThrowIfFailed(hResult);
            int count = EnsureNativeWrittenCount(written, output.Length, "DirectInput escape 輸出位元組數");
            Marshal.Copy(outputPointer, output, 0, count);
            return count;
        }
        finally
        {
            Marshal.FreeHGlobal(inputPointer);
            Marshal.FreeHGlobal(outputPointer);
        }
    }

    /// <summary>
    /// Creates an input mapper.
    /// 建立 input mapper。
    /// </summary>
    /// <returns>The newly created input mapper. 新建立的 input mapper。</returns>
    public GameInputMapper CreateInputMapper()
    {
        int hResult = Native.CreateInputMapper(out IGameInputMapper? mapper);
        GameInputException.ThrowIfFailed(hResult);
        return mapper is { } mapperValue
            ? new GameInputMapper(mapperValue)
            : throw new GameInputException(GameInputHResult.ObjectNoLongerExists);
    }

    /// <summary>
    /// Gets the extra axis indexes.
    /// 取得額外軸索引。
    /// </summary>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <returns>A new array containing the extra axis indexes. 包含額外軸索引的新陣列。</returns>
    /// <exception cref="InvalidOperationException">The index count reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的索引數量超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public byte[] GetExtraAxisIndexes(GameInputKind inputKind)
    {
        int hResult = Native.GetExtraAxisCount(inputKind, out uint count);
        GameInputException.ThrowIfFailed(hResult);
        byte[] indexes = new byte[NativeSizeGuard.EnsureCount(count, NativeSizeGuard.MaxElementCount, "額外軸索引數量")];
        if (count == 0)
        {
            return indexes;
        }

#if NET10_0_OR_GREATER
        unsafe
        {
            fixed (byte* pointer = indexes)
            {
                hResult = Native.GetExtraAxisIndexes(inputKind, count, (IntPtr)pointer);
            }
        }
#else
        hResult = Native.GetExtraAxisIndexes(inputKind, count, indexes);
#endif
        GameInputException.ThrowIfFailed(hResult);
        return indexes;
    }

    /// <summary>
    /// Gets the extra button indexes.
    /// 取得額外按鈕索引。
    /// </summary>
    /// <param name="inputKind">The GameInput input kind to query or filter. 要查詢或篩選的 GameInput 輸入種類。</param>
    /// <returns>A new array containing the extra button indexes. 包含額外按鈕索引的新陣列。</returns>
    /// <exception cref="InvalidOperationException">The index count reported by the native side exceeds the internal limit, which is treated as an anomalous device or driver report. 原生回報的索引數量超過內部上限，視為裝置或驅動程式回報異常。</exception>
    public byte[] GetExtraButtonIndexes(GameInputKind inputKind)
    {
        int hResult = Native.GetExtraButtonCount(inputKind, out uint count);
        GameInputException.ThrowIfFailed(hResult);
        byte[] indexes = new byte[NativeSizeGuard.EnsureCount(count, NativeSizeGuard.MaxElementCount, "額外按鈕索引數量")];
        if (count == 0)
        {
            return indexes;
        }

#if NET10_0_OR_GREATER
        unsafe
        {
            fixed (byte* pointer = indexes)
            {
                hResult = Native.GetExtraButtonIndexes(inputKind, count, (IntPtr)pointer);
            }
        }
#else
        hResult = Native.GetExtraButtonIndexes(inputKind, count, indexes);
#endif
        GameInputException.ThrowIfFailed(hResult);
        return indexes;
    }

    /// <summary>
    /// Creates a raw device report.
    /// 建立 raw device report。
    /// </summary>
    /// <param name="reportId">The raw device report identifier. raw device report 識別碼。</param>
    /// <param name="reportKind">The raw device report kind. raw device report 種類。</param>
    /// <returns>The newly created raw device report. 新建立的 raw device report。</returns>
    public GameInputRawDeviceReport CreateRawDeviceReport(uint reportId, GameInputRawDeviceReportKind reportKind)
    {
        int hResult = Native.CreateRawDeviceReport(reportId, reportKind, out IGameInputRawDeviceReport? report);
        GameInputException.ThrowIfFailed(hResult);
        return report is { } reportValue
            ? new GameInputRawDeviceReport(reportValue)
            : throw new GameInputException(GameInputHResult.ObjectNoLongerExists);
    }

    /// <summary>
    /// Sends raw device output.
    /// 傳送 raw device output。
    /// </summary>
    /// <param name="report">The raw device report to send or operate on. 要傳送或操作的 raw device report。</param>
    public void SendRawDeviceOutput(GameInputRawDeviceReport report)
    {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(report);
#else
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }
#endif

        int hResult = Native.SendRawDeviceOutput(report.NativeInterface);
        GameInputException.ThrowIfFailed(hResult);
    }

    /// <summary>
    /// Releases the COM reference held by this device wrapper.
    /// 釋放此裝置包裝持有的 COM 參考。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_native is not null)
        {
#if NET10_0_OR_GREATER
            _native.Value.Release();
#else
            Marshal.ReleaseComObject(_native);
#endif
            _native = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private IGameInputDevice Native
    {
        get
        {
            return _disposed
                ? throw new ObjectDisposedException(nameof(GameInputDevice))
                : _native ?? throw new ObjectDisposedException(nameof(GameInputDevice));
        }
    }

    internal static int EnsureNativeWrittenCount(uint written, int capacity, string subject)
    {
        if (written > capacity)
        {
            throw new InvalidOperationException($"{subject}（{written}）超過呼叫端提供的緩衝區大小（{capacity}）。");
        }

        return checked((int)written);
    }

    internal static bool IsForceFeedbackEffectSupported(GameInputForceFeedbackMotorInfo motor, GameInputForceFeedbackEffectKind effectKind)
    {
        return effectKind switch
        {
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackConstant => motor.IsConstantEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackRamp => motor.IsRampEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave => motor.IsSineWaveEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackSquareWave => motor.IsSquareWaveEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackTriangleWave => motor.IsTriangleWaveEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothUpWave => motor.IsSawtoothUpWaveEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackSawtoothDownWave => motor.IsSawtoothDownWaveEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackSpring => motor.IsSpringEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackFriction => motor.IsFrictionEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackDamper => motor.IsDamperEffectSupported,
            GameInputForceFeedbackEffectKind.GameInputForceFeedbackInertia => motor.IsInertiaEffectSupported,
            _ => false
        };
    }

    internal static GameInputRumbleParams MaskRumble(GameInputRumbleParams parameters, GameInputRumbleMotors supportedMotors)
    {
        return new GameInputRumbleParams
        {
            LowFrequency = SupportsRumbleMotor(supportedMotors, GameInputRumbleMotors.GameInputRumbleLowFrequency) ? parameters.LowFrequency : 0,
            HighFrequency = SupportsRumbleMotor(supportedMotors, GameInputRumbleMotors.GameInputRumbleHighFrequency) ? parameters.HighFrequency : 0,
            LeftTrigger = SupportsRumbleMotor(supportedMotors, GameInputRumbleMotors.GameInputRumbleLeftTrigger) ? parameters.LeftTrigger : 0,
            RightTrigger = SupportsRumbleMotor(supportedMotors, GameInputRumbleMotors.GameInputRumbleRightTrigger) ? parameters.RightTrigger : 0
        };
    }

    internal static bool HasActiveRumble(GameInputRumbleParams parameters)
    {
        return parameters.LowFrequency > 0
            || parameters.HighFrequency > 0
            || parameters.LeftTrigger > 0
            || parameters.RightTrigger > 0;
    }

    private static bool SupportsRumbleMotor(GameInputRumbleMotors supportedMotors, GameInputRumbleMotors motor)
    {
        return (supportedMotors & motor) == motor;
    }

    private static void ValidateRumbleStrength(GameInputRumbleParams parameters)
    {
        ValidateRumbleStrength(parameters.LowFrequency, nameof(parameters.LowFrequency));
        ValidateRumbleStrength(parameters.HighFrequency, nameof(parameters.HighFrequency));
        ValidateRumbleStrength(parameters.LeftTrigger, nameof(parameters.LeftTrigger));
        ValidateRumbleStrength(parameters.RightTrigger, nameof(parameters.RightTrigger));
    }

    private static void ValidateRumbleStrength(float value, string parameterName)
    {
        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Rumble 強度必須介於 0.0 到 1.0。");
        }
    }
}
