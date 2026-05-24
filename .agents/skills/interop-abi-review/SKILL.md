---
name: interop-abi-review
description: 當任務涉及 GameInput COM 介面、struct layout、HRESULT、callback 或 native lifetime 審查時使用。
---

使用此 skill 時：

1. 對照官方 `native/include/GameInput.h`，確認 enum、struct 欄位順序、COM IID、vtable 方法順序與 HRESULT。
2. 注意 C++ `bool` 是 1 位元組；C# struct 欄位與 COM 回傳值需明確指定 `UnmanagedType.I1`。
3. COM wrapper 必須明確釋放 RCW，不要讓 reading 或 device 長期無界持有。
4. 對常用 struct 增加 `Marshal.SizeOf<T>()` 測試。
5. 修改後執行 `dotnet test` 與 `pwsh ./eng/Verify-GameInputBindings.ps1`。
