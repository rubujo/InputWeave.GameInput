---
name: gameinput-binding-generation
description: 當需要從 Microsoft GameInput.h 重產 C# 互通層繫結，或修改繫結產生器時使用。
---

使用此技能時：

1. 確認 `Microsoft.GameInput` 版本與 `eng/gameinput-baseline.json` 一致。
2. 修改產生邏輯時，只改 `tools/InputWeave.GameInput.BindingsGenerator`。
3. 使用 `pwsh ./eng/Update-GameInputVersion.ps1 -Version <版本>`，或直接執行產生器並搭配 `--docs eng/gameinput-xml-docs.zh-TW.json` 與 `--interop-output-dir src/InputWeave.GameInput/Interop/Generated`，重產 `Interop/Generated` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置與 `gameinput-abi-manifest.json`。COM 介面依 TFM 產生兩種形式（見下方「net10 裸 vtable 投影」）。
4. ABI 檢查必須涵蓋列舉值、結構欄位順序、COM IID、Vtable 方法順序、HRESULT 與回呼委派；`net10` 的 vtable 方法順序由 `eng/Verify-GameInputBindings.ps1` 自動比對 `gameinput-abi-manifest.json`，不得只靠編譯成功判斷正確性。
5. C++ `bool` 對應必須確認為 1 位元組；`net48` 的 C# 結構欄位與 COM 回傳值需明確指定 `UnmanagedType.I1`；`net10` 的裸 vtable 方法回傳型別用 `byte`，wrapper 內以 `!= 0` 轉換。
6. 不要手改產生式互通層；若產生結果不正確，修改 `tools/InputWeave.GameInput.BindingsGenerator` 後重產。
7. 產生檔必須使用 File-scoped Namespace，不得輸出 `#pragma warning disable`。
8. 產生檔必須包含完整 XML 文件註解；若缺少 `summary`、`param` 或 `returns`，修改 `eng/gameinput-xml-docs.zh-TW.json` 與產生器後重產。
9. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1`、`dotnet test InputWeave.GameInput.slnx -c Release --no-build` 與 `pwsh ./eng/Validate-TextEncoding.ps1`；vtable slot 順序或簽章寫錯不會編譯失敗，而是靜默呼叫錯方法或記憶體毀損，因此務必用真實 GameInput.dll 跑過 `dotnet test`（本機若已安裝 GameInput runtime，測試不會走 `Inconclusive` 分支），不能只看編譯結果。

## net10 裸 vtable 投影（NativeAOT 相容互通層，已採用）

`net10.0-windows` 的 COM 介面**不使用** `[ComImport]` 或來源產生式 `[GeneratedComInterface]`／`ComWrappers`，而是仿照 `TerraFX.Interop.Windows`／`DirectN` 的做法，把每個介面投影成**裸 vtable 結構 + `delegate* unmanaged[Stdcall]<...>` 函式指標**，呼叫端手動 `AddRef`/`Release`。`net48` 維持 `[ComImport]` 完全不變（產生器輸出拆成 `GameInputNativeInterfaces.NetFramework.g.cs`／`GameInputNativeInterfaces.Net10.g.cs` 兩份）。

**為何不用 `GeneratedComInterface`（歷史紀錄，之前確認的限制）**：

- **陣列參數需要 `DisableRuntimeMarshalling`**：`GeneratedComInterface` 對 `[Out] T[] stateArray` 這類陣列封送要求整個組件套用 `[assembly: DisableRuntimeMarshalling]`，但這會連帶讓 `GameInputRuntimeLoader.cs`（`Marshal.GetDelegateForFunctionPointer<GameInputInitializeDelegate>`）與 `GameInputCallbacks.g.cs` 的 `[UnmanagedFunctionPointer]` 回呼委派全部編譯失敗（`CA1420`）。要嘛把這些委派也改寫成 blittable 的 `delegate* unmanaged<...>` 函式指標，要嘛把陣列方法改成 `IntPtr` 緩衝區（呼叫端用 `fixed` 釘選 managed 陣列取代 `[MarshalUsing(CountElementName=...)]`），避免整組件停用執行階段封送處理。
- **委派回呼參數不可再用 `[GeneratedComInterface]` 介面型別**：`GameInputDeviceCallback`／`GameInputReadingCallback` 等原生回呼委派若保留 `IGameInputDevice`／`IGameInputReading` 為參數型別，`Marshal.GetFunctionPointerForDelegate` 產生的原生可呼叫進入點無法正確把原生 COM 指標轉換成 `GeneratedComInterface` 型別的 managed 包裝——實測會導致 `GetDeviceInfo()` 等後續呼叫拿到無效指標並丟出 `NullReferenceException`。要修正必須把回呼委派的 COM 介面參數改成 `IntPtr`，並在 `GameInputClient.cs` 的靜態回呼處理常式內用 `StrategyBasedComWrappers` 手動包裝，這代表 `GameInputCallbacks.g.cs` 也需要依 TFM 拆成兩份。
- **巢狀 COM 物件缺乏確定性釋放路徑**：`Marshal.ReleaseComObject` 對 `GeneratedComInterface` 型別完全不支援（`SYSLIB1099`）。只有手動呼叫 `StrategyBasedComWrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance)` 包裝的物件，才能透過 `(native as IDisposable)?.Dispose()` 確定性釋放（適用於 `GameInputClient` 頂層的 `IGameInput`）。但 `IGameInputDevice`／`IGameInputReading`／`IGameInputForceFeedbackEffect` 等透過其他 COM 方法 `out` 參數自動封送產生的介面，沒有可用的方式插入 `UniqueInstance`，只能依賴 GC 終結器非決定性回收，牴觸本檔案「COM 物件與原生讀取資料必須有明確 `IDisposable` 生命週期」的要求。

裸 vtable 投影完全避開這三個問題：不需要 `DisableRuntimeMarshalling`、回呼參數是原始指標零封送成本、`Release()` 直接呼叫 vtable slot 決定性釋放。GameInput 官方文件也把自己定位為「COM-lite / nano-COM」（僅借用 `IUnknown` vtable 形狀做參考計數，不使用完整 COM runtime），與此模式天生契合。

**產生規則**（`tools/InputWeave.GameInput.BindingsGenerator/Program.cs` 的 `WriteVtableInterfaces`／`VtableMethodPlan`／`VtableParameterParser`）：

- 每個介面產生 `{Name}Vtbl`（`[StructLayout(LayoutKind.Sequential)]`，欄位依 `GameInput.h` 原始 vtable 順序排列，前三個固定是 `QueryInterface`/`AddRef`/`Release`）與 `{Name}`（`internal readonly unsafe struct`，只包一個 `Pointer` 欄位，透過 `Vtbl->Method(Pointer, ...)` 呼叫）。
- 借用型（in）COM 介面參數傳 `.Pointer`（`null` 用 `IntPtr.Zero`）；`out` COM 介面參數在 vtable 層是 `void**`，wrapper 讀出原始指標後包裝成新的 `{Type}((IntPtr)raw)`——依 COM 慣例，這是呼叫端擁有的新參考，由呼叫端負責 `Release()`。
- `ref`/`out` 的 blittable 結構或純量參數（如 `AppLocalDeviceId`、`GameInputMouseState`、`ulong callbackToken`）在 vtable 層是 `T*`，wrapper 用 `fixed (T* p = &value)` 直接對參數本身取址呼叫，不需要額外複製。
- 陣列＋計數參數（`GetControllerAxisState` 等）與委派回呼參數（`RegisterReadingCallback` 等）一律改成 `IntPtr`：陣列由高階類別（`GameInputReading.cs`／`GameInputDevice.cs`）用 `fixed` 釘選後傳入；回呼由 `GameInputClient.cs` 用 `[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]` 靜態方法 + `&OnXxxCallback` 取原始函式指標傳入，取代 `Marshal.GetFunctionPointerForDelegate`。
- `string` 參數（`FindDeviceFromPlatformString`）由 wrapper 用 `fixed (char* p = value)` 轉成原生緩衝區。
- 消費端（`GameInputClient.cs`／`GameInputDevice.cs`／`GameInputReading.cs`／`GameInputDispatcher.cs`／`GameInputForceFeedbackEffect.cs`／`GameInputMapper.cs`／`GameInputRawDeviceReport.cs`）的 `_native` 欄位型別維持 `IGameInputXxx?`（`net10` 下等同 `Nullable<struct>`，`net48` 下是可為 Null 的介面參考，語法相同不需要 `#if`）；`Dispose()` 依 TFM 分流呼叫 `_native.Value.Release()`（`net10`）或 `Marshal.ReleaseComObject(_native)`（`net48`）。
- `GameInputClient.Create()` 在 `net10` 下用 `new IGameInput(nativePointer); native.AddRef();` 取代 `Marshal.GetObjectForIUnknown`，語意與 `net48` 對稱（`GameInputInitialize` 交出的參考在 `finally` 釋放，包裝物件自己持有另一個 `AddRef`）。

不要在此設計上重新嘗試 `GeneratedComInterface`／`ComWrappers`；若未來 .NET 對巢狀 COM 物件開放可自訂的確定性釋放路徑，才值得重新評估。
