---
name: gameinput-binding-generation
description: 當需要從 Microsoft GameInput.h 重產 C# 互通層繫結，或修改繫結產生器時使用。
---

使用此技能時：

1. 確認 `Microsoft.GameInput` 版本與 `eng/gameinput-baseline.json` 一致。
2. 修改產生邏輯時，只改 `tools/InputWeave.GameInput.BindingsGenerator`。
3. 使用 `pwsh ./eng/Update-GameInputVersion.ps1 -Version <版本>`，或直接執行產生器並搭配 `--docs eng/gameinput-xml-docs.zh-TW.json` 與 `--interop-output-dir src/InputWeave.GameInput/Interop/Generated`，重產 `Interop/Generated` 下的列舉、常數、HRESULT、IID、回呼委派、結構配置、COM Interface 與 `gameinput-abi-manifest.json`。
4. ABI 檢查必須涵蓋列舉值、結構欄位順序、COM IID、Vtable 方法順序、HRESULT 與回呼委派。
5. C++ `bool` 對應必須確認為 1 位元組；C# 結構欄位與 COM 回傳值需明確指定 `UnmanagedType.I1`。
6. 不要手改產生式互通層；若產生結果不正確，修改 `tools/InputWeave.GameInput.BindingsGenerator` 後重產。
7. 產生檔必須使用 File-scoped Namespace，不得輸出 `#pragma warning disable`。
8. 產生檔必須包含完整 XML 文件註解；若缺少 `summary`、`param` 或 `returns`，修改 `eng/gameinput-xml-docs.zh-TW.json` 與產生器後重產。
9. 執行 `pwsh ./eng/Verify-GameInputBindings.ps1`、`dotnet test InputWeave.GameInput.slnx -c Release --no-build` 與 `pwsh ./eng/Validate-TextEncoding.ps1`。
