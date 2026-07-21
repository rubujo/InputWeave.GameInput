# GameInput 版本報告

目前基準版本：`Microsoft.GameInput` `3.4.259`

目前包裝程式庫版本：`InputWeave.GameInput v0.0.1`，NuGet / MSBuild 版本為 `0.0.1`。

- API 版本：`3`
- NuGet 套件 SHA256：`D763A7CC2F72768B6206A2714681DEAC369FC87C4A8351689D051A22D9A2678E`
- `native/include/GameInput.h` SHA256：`828E1247795602681D0496CA94958CFF38A2FF0B673690512D46F947395D0553`
- `redist/GameInputRedist.msi` SHA256：`6AD6AFC369A9CC936AAB61DC945D2F8EE9B5D4E31876470CD4D9F6597BB7835D`

低階互通層來源：`src/InputWeave.GameInput/Interop/Generated/` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置、COM 介面與 `gameinput-abi-manifest.json` 均由目前基準的 `GameInput.h` 產生。

## 追版流程

1. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認 NuGet 是否有新版。
2. 若有新版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`。
3. 檢查 `Directory.Packages.props`、`eng/gameinput-baseline.json`、`src/InputWeave.GameInput/Interop/Generated/` 下的 `.g.cs`、`gameinput-abi-manifest.json` 與本報告。
4. 執行 `dotnet build`、`dotnet test`、`pwsh ./eng/Verify-GameInputBindings.ps1`、`pwsh ./eng/Verify-GameInputCoverage.ps1`。
5. 若 GameInput.h 公開 API 有新增或異動，先更新產生器映射，再更新覆蓋率與版本文件。
