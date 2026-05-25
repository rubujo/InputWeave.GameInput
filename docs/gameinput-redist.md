# GameInput Redist 發佈注意事項

Microsoft 官方 `Microsoft.GameInput` NuGet 套件會提供最新 `GameInput.h`、原生 lib 與 PC 用 `GameInputRedist.msi`。這個 wrapper 的 NuGet 套件不會重新散佈 MSI、redist DLL 或任何原生 shim，也不會在使用者電腦上自動安裝 redist。

InputWeave 目前採取 managed-only loader，預設對齊 Microsoft C++ loader 的 runtime selection 行為。載入候選來源如下：

- Windows System32 內的 inbox `GameInput.dll`。
- Windows System32 內的 `GameInputRedist.dll`。
- `HKLM\SOFTWARE\Microsoft\GameInput\RedistDir` 指向目錄中的 `GameInputRedist.dll`。

若找到 redist runtime，且 redist 檔案版本大於或等於 inbox runtime，InputWeave 會優先載入 redist runtime；若 redist 不存在或版本較舊，則載入 inbox runtime。這讓已安裝的 Microsoft redist 可以被使用，同時不需要在 wrapper NuGet 中散佈任何 Microsoft runtime 檔案。

載入安全邊界仍然是 DLL hijack 防護：InputWeave 不從應用程式目錄、目前工作目錄或 `PATH` 載入 `GameInput.dll` / `GameInputRedist.dll`。System32 候選只允許 System32 搜尋路徑；registry redist 候選只允許 DLL 所在目錄與 System32 解析相依性。需要診斷載入來源時，可使用 `GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info)` 取得候選清單、選擇結果、HRESULT 與 Win32 錯誤碼。

發佈 PC 應用程式時請遵守下列規則：

- 發佈端安裝程式必須安裝 `GameInputRedist.msi`，讓目標機器取得最新 GameInput runtime。
- 若目標機器已安裝較新的 GameInput runtime，Microsoft redist 會避免降版。
- 這個 repo 的 `eng/gameinput-baseline.json` 只保存 redist 的 SHA256，供發佈與追版流程確認來源一致。
- 若未來 managed loader 在特定發佈模式中不足，例如 single-file 或更深的 native 診斷需求，再另行規劃 resolver 或 native shim；native shim 不是目前預設發佈形狀。
