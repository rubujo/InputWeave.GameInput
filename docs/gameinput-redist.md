# GameInput Redist 發佈注意事項

Microsoft 官方 `Microsoft.GameInput` NuGet 套件會提供最新 `GameInput.h`、原生 lib 與 PC 用 `GameInputRedist.msi`。這個 wrapper 的 NuGet 套件不會重新散佈 MSI，也不會在使用者電腦上自動安裝 redist。

發佈 PC 應用程式時請遵守下列規則：

- 發佈端安裝程式必須安裝 `GameInputRedist.msi`，讓目標機器取得最新 GameInput runtime。
- 若目標機器已安裝較新的 GameInput runtime，Microsoft redist 會避免降版。
- 這個 repo 的 `eng/gameinput-baseline.json` 只保存 redist 的 SHA256，供發佈與追版流程確認來源一致。
- 若未來做單檔發佈或 native shim，必須另外驗證 native DLL 是否進入發佈圖與 bundle；不要只假設 `IncludeNativeLibrariesForSelfExtract` 已足夠。
