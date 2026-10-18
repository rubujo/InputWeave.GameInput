using System.Runtime.InteropServices;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class NativeSizeGuardTests
{
    [TestMethod]
    public void EnsureCountReturnsValueWhenWithinLimit()
    {
        Assert.AreEqual(0, NativeSizeGuard.EnsureCount(0, NativeSizeGuard.MaxElementCount, "測試數量"));
        Assert.AreEqual(
            NativeSizeGuard.MaxElementCount,
            NativeSizeGuard.EnsureCount((ulong)NativeSizeGuard.MaxElementCount, NativeSizeGuard.MaxElementCount, "測試數量"));
    }

    [TestMethod]
    public void EnsureCountThrowsInvalidOperationExceptionWhenExceedingLimit()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => NativeSizeGuard.EnsureCount((ulong)NativeSizeGuard.MaxElementCount + 1, NativeSizeGuard.MaxElementCount, "測試數量"));
    }

    [TestMethod]
    public void EnsureCountThrowsInvalidOperationExceptionWhenValueExceedsIntMaxValue()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => NativeSizeGuard.EnsureCount((ulong)int.MaxValue + 1, NativeSizeGuard.MaxElementCount, "測試數量"));
    }

    [TestMethod]
    public void RawDeviceReportEnsureNativeWrittenCountRejectsOversizedResult()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => GameInputRawDeviceReport.EnsureNativeWrittenCount(4, 3, "測試寫入數量"));
    }

    [TestMethod]
    public void DirectInputEscapeEnsureNativeWrittenCountRejectsOversizedResult()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(
            () => GameInputDevice.EnsureNativeWrittenCount(4, 3, "測試寫入數量"));
    }

}
