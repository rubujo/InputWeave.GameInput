# GameInput Wrapper Agent 政策

Agent 修改本 repo 時必須遵守：

- 任何 GameInput API 或 interop 變更前，先執行或檢查 `eng/Check-GameInputVersion.ps1`。
- 修改低階繫結後，必須執行 `eng/Verify-GameInputBindings.ps1`。
- 公開 API 與 XML 文件註解使用正體中文台灣用語。
- 不要手改產生檔；應修改 `tools/InputWeave.GameInput.BindingsGenerator` 後重產。
- PowerShell 腳本必須支援 `pwsh` 7.4+，檔案使用 UTF-8 無 BOM 與 CRLF。
- VS/MSBuild 檔案可保留 Visual Studio 或 .NET CLI 建立時既有的 UTF-8 BOM；不要用批次正規化強制移除。
- 不要把 `GameInputRedist.msi` 包進 wrapper NuGet；只記錄雜湊並在應用程式發佈文件說明安裝責任。
