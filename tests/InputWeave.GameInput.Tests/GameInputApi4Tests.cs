using System.Runtime.InteropServices;
using System.Text;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GameInputApi4Tests
{
    [TestMethod]
    public unsafe void TouchAndLightingLayoutsMatchHeader()
    {
        Assert.AreEqual(3, sizeof(GameInputColor));
        Assert.AreEqual(24, sizeof(GameInputTouchContact));
        Assert.AreEqual(16, sizeof(GameInputTouchSurfaceInfo));
        Assert.AreEqual(8, Marshal.OffsetOf<GameInputTouchContact>(nameof(GameInputTouchContact.ScreenX)).ToInt32());
        Assert.AreEqual(20, Marshal.OffsetOf<GameInputTouchContact>(nameof(GameInputTouchContact.PadY)).ToInt32());
        Assert.AreEqual(12, Marshal.OffsetOf<GameInputTouchSurfaceInfo>(nameof(GameInputTouchSurfaceInfo.Type)).ToInt32());
        Assert.AreEqual(uint.MaxValue, typeof(GameInputConstants).GetField(nameof(GameInputConstants.GAMEINPUT_LIGHTING_GROUP_ORDER_INDEX_ALL))!.GetRawConstantValue());
        Assert.AreEqual(new Guid("08c707c2-66bb-406c-a84a-dfe085120a92"), GameInputConstants.GAMEINPUT_HAPTIC_LOCATION_GRIP_LEFT);
        Assert.AreEqual((int)(GameInputConstants.GameInputGamepadLayoutStandard | GameInputConstants.GameInputGamepadModulePaddles4), typeof(GameInputConstants).GetField(nameof(GameInputConstants.GameInputGamepadLayoutElite))!.GetRawConstantValue());
    }

    [TestMethod]
    public void Utf8GetterRetriesGrowthAndCopiesUnicode()
    {
        byte[] text = Encoding.UTF8.GetBytes("控制器 Ω\0");
        int calls = 0;
        string? result = GameInputDevice.ReadString((capacity, buffer) =>
        {
            calls++;
            if (calls == 1)
            {
                return 2;
            }
            if (capacity >= text.Length)
            {
                Marshal.Copy(text, 0, buffer, text.Length);
            }
            return (uint)text.Length;
        });
        Assert.AreEqual("控制器 Ω", result);
        Assert.AreEqual(3, calls);
        Assert.IsNull(GameInputDevice.ReadString(static (_, _) => 0));
    }

    [TestMethod]
    public void Utf8GetterRejectsOversizedAndUnstableLengths()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => GameInputDevice.ReadString(static (_, _) => uint.MaxValue));
        int calls = 0;
        Assert.ThrowsExactly<InvalidOperationException>(() => GameInputDevice.ReadString((_, _) => (uint)++calls));
        Assert.AreEqual(4, calls);
    }

    [TestMethod]
    public void NativeArrayCopiesOnlyWrittenElementsAndRetriesGrowth()
    {
        int copies = 0;
        uint[] values = GameInputDevice.ReadArray<uint>(static () => 2, (_, buffer) =>
        {
            copies++;
            if (copies == 1)
            {
                return 3;
            }
            Marshal.WriteInt32(buffer, 42);
            return 1;
        });
        CollectionAssert.AreEqual(new uint[] { 42 }, values);
        Assert.AreEqual(2, copies);
        Assert.ThrowsExactly<InvalidOperationException>(() => GameInputDevice.ReadArray<uint>(static () => uint.MaxValue, static (_, _) => 0));
        Assert.ThrowsExactly<InvalidOperationException>(() => GameInputDevice.ReadArray<uint>(static () => 1, static (_, _) => 2));
    }

    [TestMethod]
    public void Api4SnapshotEqualityIncludesDynamicCapabilities()
    {
        GameInputDeviceInfoSnapshot first = new()
        {
            SupportedLightingGroups = GameInputLightingGroups.GameInputLightingGroupCenterLight,
            XInputDeviceIndex = 2,
            FeatureReports = [new GameInputRawDeviceReportInfo { Id = 7 }],
            TouchSurfaces = [new GameInputTouchSurfaceInfo { MaxContactCount = 2 }]
        };
        GameInputDeviceInfoSnapshot copy = first with
        {
            FeatureReports = [new GameInputRawDeviceReportInfo { Id = 7 }],
            TouchSurfaces = [new GameInputTouchSurfaceInfo { MaxContactCount = 2 }]
        };
        Assert.AreEqual(first, copy);
        Assert.AreEqual(first.GetHashCode(), copy.GetHashCode());
        Assert.AreNotEqual(first, first with { XInputDeviceIndex = 1 });
        Assert.AreNotEqual(first, first with { TouchSurfaces = [] });
        Assert.AreNotEqual(first, first with { FeatureReports = [] });
        Assert.AreNotEqual(first, first with { SupportedLightingGroups = 0 });
    }

    [TestMethod]
    public unsafe void LightingAndXInputUseApi4VtableAndPreserveOwnership()
    {
        IGameInputDeviceVtbl table = default;
        table.Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
        table.SetLightingColor = (delegate* unmanaged[Stdcall]<IntPtr, GameInputColor, GameInputLightingGroups, uint, void>)Marshal.GetFunctionPointerForDelegate(s_color);
        table.ApplyLightingColors = (delegate* unmanaged[Stdcall]<IntPtr, void>)Marshal.GetFunctionPointerForDelegate(s_apply);
        table.ApplyLightingSystemDefault = (delegate* unmanaged[Stdcall]<IntPtr, void>)Marshal.GetFunctionPointerForDelegate(s_reset);
        table.GetXInputDeviceIndex = (delegate* unmanaged[Stdcall]<IntPtr, uint*, byte>)Marshal.GetFunctionPointerForDelegate(s_xinput);
        IGameInputDeviceVtbl* tablePointer = &table;
        IntPtr self = (IntPtr)(&tablePointer);
        s_releases = 0;
        s_applied = false;
        s_resetCalled = false;
        using (GameInputDevice device = new(new IGameInputDevice(self)))
        {
            device.SetLightingColor(new GameInputColor { R = 17, G = 89, B = 231 }, GameInputLightingGroups.GameInputLightingGroupCenterLight);
            Assert.AreEqual((byte)17, s_lastColor.R);
            Assert.AreEqual((byte)89, s_lastColor.G);
            Assert.AreEqual((byte)231, s_lastColor.B);
            Assert.AreEqual(GameInputLightingGroups.GameInputLightingGroupCenterLight, s_lastGroup);
            Assert.AreEqual(uint.MaxValue, s_lastIndex);
            device.ApplyLightingColors();
            device.ApplyLightingSystemDefault();
            Assert.IsTrue(s_applied);
            Assert.IsTrue(s_resetCalled);
            Assert.IsTrue(device.TryGetXInputDeviceIndex(out uint index));
            Assert.AreEqual(3u, index);
        }
        Assert.AreEqual(1, s_releases);
    }

    [TestMethod]
    public unsafe void TouchReadingCopiesContactsAndSystemButtons()
    {
        IGameInputReadingVtbl table = default;
        table.Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_release);
        table.GetTouchContactCount = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint>)Marshal.GetFunctionPointerForDelegate(s_contactCount);
        table.GetTouchSurfaceState = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, IntPtr, uint>)Marshal.GetFunctionPointerForDelegate(s_contacts);
        table.GetSystemButtonState = (delegate* unmanaged[Stdcall]<IntPtr, GameInputSystemButtons*, byte>)Marshal.GetFunctionPointerForDelegate(s_systemButtons);
        IGameInputReadingVtbl* tablePointer = &table;
        using GameInputReading reading = new(new IGameInputReading((IntPtr)(&tablePointer)));
        GameInputTouchContact[] contacts = reading.GetTouchSurfaceState(5);
        Assert.HasCount(1, contacts);
        Assert.AreEqual(123UL, contacts[0].ContactId);
        Assert.AreEqual(-17, contacts[0].ScreenX);
        Assert.AreEqual(800u, contacts[0].PadY);
        Assert.IsTrue(reading.TryGetSystemButtonState(out GameInputSystemButtons buttons));
        Assert.AreEqual((GameInputSystemButtons)1, buttons);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseFunction(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void ColorFunction(IntPtr self, GameInputColor color, GameInputLightingGroups group, uint index);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void ApplyFunction(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate byte XInputFunction(IntPtr self, IntPtr index);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ContactCountFunction(IntPtr self, uint surfaceIndex);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ContactsFunction(IntPtr self, uint surfaceIndex, uint capacity, IntPtr contacts);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate byte SystemButtonsFunction(IntPtr self, IntPtr buttons);
    private static readonly ReleaseFunction s_release = static _ =>
    {
        s_releases++;
        return 0;
    };
    private static readonly ColorFunction s_color = static (_, color, group, index) =>
    {
        s_lastColor = color;
        s_lastGroup = group;
        s_lastIndex = index;
    };
    private static readonly ApplyFunction s_apply = static _ => s_applied = true;
    private static readonly ApplyFunction s_reset = static _ => s_resetCalled = true;
    private static readonly XInputFunction s_xinput = static (_, index) =>
    {
        Marshal.WriteInt32(index, 3);
        return 1;
    };
    private static readonly ContactCountFunction s_contactCount = static (_, surface) => surface == 5 ? 2u : 0u;
    private static readonly ContactsFunction s_contacts = static (_, surface, capacity, contacts) =>
    {
        if (surface != 5 || capacity != 2)
        {
            return 0;
        }
        Marshal.StructureToPtr(new GameInputTouchContact { ContactId = 123, ScreenX = -17, PadY = 800 }, contacts, false);
        return 1;
    };
    private static readonly SystemButtonsFunction s_systemButtons = static (_, buttons) =>
    {
        Marshal.WriteInt32(buttons, 1);
        return 1;
    };
    private static GameInputColor s_lastColor;
    private static GameInputLightingGroups s_lastGroup;
    private static uint s_lastIndex;
    private static bool s_applied;
    private static bool s_resetCalled;
    private static int s_releases;
}
