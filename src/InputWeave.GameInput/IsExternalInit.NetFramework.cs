// net48 的參考組件沒有 IsExternalInit；補上編譯器需要的型別讓 init 存取子可用。
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit;
#endif
