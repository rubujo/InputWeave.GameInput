using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InputWeave.GameInput.Tests")]
[assembly: InternalsVisibleTo("InputWeave.GameInput.Benchmarks")]

#if NET8_0_OR_GREATER
// GameInput 只存在於 Windows；以一般 net8.0／net10.0 目標框架發佈是為了讓 Godot、MonoGame 等跨平台專案可以參考，
// 平台相容性分析器（CA1416）會在未檢查 OperatingSystem.IsWindows() 就呼叫時於編譯期提醒。
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
