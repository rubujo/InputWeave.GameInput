---
name: gameinput-binding-generation
description: Use when regenerating C# interop bindings from Microsoft GameInput.h or changing the bindings generator.
---

使用此 skill 時：

1. 確認 `Microsoft.GameInput` 版本與 `eng/gameinput-baseline.json` 一致。
2. 修改產生邏輯時，只改 `tools/InputWeave.GameInput.BindingsGenerator`。
3. 使用 `pwsh ./eng/Update-GameInputVersion.ps1 -Version <版本>` 或直接執行 generator 搭配 `--docs eng/gameinput-xml-docs.zh-TW.json` 與 `--interop-output-dir src/InputWeave.GameInput/Interop/Generated`，重產 `Interop/Generated` 下的 enum、constants、HRESULT、IID、callback delegate、struct layout、COM interface 與 `gameinput-abi-manifest.json`。
4. ABI 檢查必須涵蓋 enum 值、struct 欄位順序、COM IID、vtable method order、HRESULT 與 callback delegate。
5. C++ `bool` 對應必須確認為 1 位元組；C# struct 欄位與 COM 回傳值需明確指定 `UnmanagedType.I1`。
6. 不要手改 generated interop；若產生結果不正確，修改 `tools/InputWeave.GameInput.BindingsGenerator` 後重產。
7. 產生檔必須使用 file-scoped namespace，不得輸出 `#pragma warning disable`。
8. 產生檔必須包含完整 XML 文件註解；若缺少 summary、param 或 returns，修改 `eng/gameinput-xml-docs.zh-TW.json` 與 generator 後重產。
9. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1`、`dotnet test InputWeave.GameInput.slnx -c Release --no-build` 與 `pwsh ./eng/Validate-TextEncoding.ps1`。
