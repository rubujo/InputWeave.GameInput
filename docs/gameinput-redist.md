# GameInput 可轉散發套件發佈注意事項

Microsoft 官方 `Microsoft.GameInput` NuGet 套件會提供最新 `GameInput.h`、原生程式庫與 Windows PC 用 `GameInputRedist.msi`。這個包裝套件不會重新散佈 MSI、可轉散發 DLL 或任何原生橋接 DLL，也不會在使用者電腦上自動安裝可轉散發套件。

InputWeave 目前支援一般 .NET Framework 與 .NET Windows 應用程式。`net10.0-windows` 已實際跑過 `dotnet publish -p:PublishAot=true` 端對端驗證（獨立探測專案 + 實體硬體，詳見 README「支援範圍」段落）；不宣告 single-file 發佈相容性。

InputWeave 目前採取純受控載入器，預設對齊 Microsoft C++ 載入器的執行階段選擇行為。載入候選來源如下：

- Windows System32 內建的 `GameInput.dll`。
- Windows System32 內的 `GameInputRedist.dll`。
- `HKLM\SOFTWARE\Microsoft\GameInput\RedistDir` 指向目錄中的 `GameInputRedist.dll`。

若找到可轉散發執行階段，且其檔案版本大於或等於 Windows 內建執行階段，InputWeave 會優先載入可轉散發執行階段；若可轉散發執行階段不存在或版本較舊，則載入 Windows 內建執行階段。這讓已安裝的 Microsoft 可轉散發套件可以被使用，同時不需要在包裝套件中散佈任何 Microsoft 執行階段檔案。

載入安全邊界仍然是 DLL 劫持防護：InputWeave 不從應用程式目錄、目前工作目錄或 `PATH` 載入 `GameInput.dll` / `GameInputRedist.dll`。System32 候選只允許 System32 搜尋路徑；登錄檔可轉散發候選只允許 DLL 所在目錄與 System32 解析相依性。需要診斷載入來源時，可使用 `GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info)` 取得候選清單、選擇結果、HRESULT 與 Win32 錯誤碼。

發佈 PC 應用程式時請遵守下列規則：

- 發佈端安裝程式必須安裝 `GameInputRedist.msi`，讓目標機器取得最新 GameInput 執行階段。
- 若目標機器已安裝較新的 GameInput 執行階段，Microsoft 可轉散發套件會避免降版。
- 這個儲存庫的 `eng/gameinput-baseline.json` 只保存可轉散發套件的 SHA256，供發佈與追版流程確認來源一致。
- 若未來受控載入器在特定發佈模式中不足，例如 NativeAOT、trimming、single-file 或更深入的原生診斷需求，再另行規劃解析器或原生橋接 DLL；原生橋接 DLL 不是目前預設發佈形狀。
