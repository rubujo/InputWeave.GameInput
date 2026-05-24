# GameInput 版本報告

目前基準版本：`Microsoft.GameInput` `3.4.218`

目前 wrapper 版本：`InputWeave.GameInput v0.0.1`，NuGet / MSBuild 版本為 `0.0.1`。

- API version：`3`
- NuGet 套件 SHA256：`7F33C11D81D0286F649B4238877A94D116C19767713EF5D0E2DDC542CC411C26`
- `native/include/GameInput.h` SHA256：`828E1247795602681D0496CA94958CFF38A2FF0B673690512D46F947395D0553`
- `redist/GameInputRedist.msi` SHA256：`9D926C3FAB9E21509F40971CBA41CFBFA863216E09D1D82AD3F7D9C4EA936CE2`

低階 interop 來源：`src/InputWeave.GameInput/Interop/Generated/` 下的 enum、constants、HRESULT、IID、callback delegate、struct layout、COM interface 與 `gameinput-abi-manifest.json` 均由目前 baseline 的 `GameInput.h` 產生。

## 追版流程

1. 執行 `pwsh ./eng/Check-GameInputVersion.ps1 -FailOnOutdated` 確認 NuGet 是否有新版。
2. 若有新版，執行 `pwsh ./eng/Update-GameInputVersion.ps1`。
3. 檢查 `Directory.Packages.props`、`eng/gameinput-baseline.json`、`src/InputWeave.GameInput/Interop/Generated/` 下的 `.g.cs`、`gameinput-abi-manifest.json` 與本報告。
4. 執行 `dotnet build`、`dotnet test`、`pwsh ./eng/Verify-GameInputBindings.ps1`、`pwsh ./eng/Verify-GameInputCoverage.ps1`。
5. 若 GameInput.h 公開 API 有新增或異動，先更新 generator 映射，再更新 coverage 與版本文件。
