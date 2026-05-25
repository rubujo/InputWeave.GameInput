---
name: gameinput-binding-generation
description: Use when regenerating C# interop bindings from Microsoft GameInput.h or changing the bindings generator.
---

使用此 skill 時：

1. 確認 `Microsoft.GameInput` 版本與 `eng/gameinput-baseline.json` 一致。
2. 修改產生邏輯時，只改 `tools/InputWeave.GameInput.BindingsGenerator`。
3. 使用 `pwsh ./eng/Update-GameInputVersion.ps1 -Version <版本>` 或直接執行 generator 搭配 `--docs eng/gameinput-xml-docs.zh-TW.json` 與 `--interop-output-dir src/InputWeave.GameInput/Interop/Generated`，重產 `Interop/Generated` 下的 enum、constants、HRESULT、IID、callback delegate、struct layout、COM interface 與 `gameinput-abi-manifest.json`。
4. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1`。
5. 不要手改 generated interop；若產生結果不正確，修改 `tools/InputWeave.GameInput.BindingsGenerator` 後重產。
6. 產生檔必須使用 file-scoped namespace，不得輸出 `#pragma warning disable`。
7. 產生檔必須包含完整 XML 文件註解；若缺少 summary、param 或 returns，修改 `eng/gameinput-xml-docs.zh-TW.json` 與 generator 後重產。
8. 產生檔必須維持 UTF-8 無 BOM 與 CRLF。
