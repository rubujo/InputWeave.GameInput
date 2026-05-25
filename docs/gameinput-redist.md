# GameInput Redist 發佈注意事項

Microsoft 官方 `Microsoft.GameInput` NuGet 套件會提供最新 `GameInput.h`、原生 lib 與 PC 用 `GameInputRedist.msi`。這個 wrapper 的 NuGet 套件不會重新散佈 MSI、redist DLL 或任何原生 shim，也不會在使用者電腦上自動安裝 redist。

InputWeave 目前採取安全優先的 managed-only 載入策略：`GameInput.dll` 的 P/Invoke 搜尋路徑限制在 Windows System32，用來降低應用程式目錄或目前工作目錄中同名 DLL 造成的 hijack 風險。

這個策略不等同於 Microsoft C++ loader parity。本 wrapper 不會讀取 `HKLM\SOFTWARE\Microsoft\GameInput\RedistDir`、不會載入 `GameInputRedist.dll`，也不會在 inbox runtime 與 redist runtime 之間做版本選擇。

發佈 PC 應用程式時請遵守下列規則：

- 發佈端安裝程式必須安裝 `GameInputRedist.msi`，讓目標機器取得最新 GameInput runtime。
- 若目標機器已安裝較新的 GameInput runtime，Microsoft redist 會避免降版。
- 這個 repo 的 `eng/gameinput-baseline.json` 只保存 redist 的 SHA256，供發佈與追版流程確認來源一致。
- 若未來需要 redist runtime selection、載入來源診斷或單檔發佈中的原生載入協助，必須另行規劃 loader/resolver 設計與套件驗證。
