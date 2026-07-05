using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputReadingTests
{
    [TestMethod]
    public void GetControllerAxisStateWithBufferWritesExpectedData()
    {
        float[] mockAxes = [0.1f, 0.2f, 0.3f];
        MockReading readingStub = new()
        {
            Axes = mockAxes
        };

        using GameInputReading reading = new(readingStub);
        float[] buffer = new float[5];

        uint written = reading.GetControllerAxisState(buffer);

        Assert.AreEqual(3u, written);
        Assert.AreEqual(0.1f, buffer[0]);
        Assert.AreEqual(0.2f, buffer[1]);
        Assert.AreEqual(0.3f, buffer[2]);
        Assert.AreEqual(0f, buffer[3]);
    }

    [TestMethod]
    public void GetControllerButtonStateWithBufferWritesExpectedData()
    {
        byte[] mockButtons = [1, 0, 1];
        MockReading readingStub = new()
        {
            Buttons = mockButtons
        };

        using GameInputReading reading = new(readingStub);
        byte[] buffer = new byte[5];

        uint written = reading.GetControllerButtonState(buffer);

        Assert.AreEqual(3u, written);
        Assert.AreEqual(1, buffer[0]);
        Assert.AreEqual(0, buffer[1]);
        Assert.AreEqual(1, buffer[2]);
        Assert.AreEqual(0, buffer[3]);
    }

    [TestMethod]
    public void GetControllerSwitchStateWithBufferWritesExpectedData()
    {
        GameInputSwitchPosition[] mockSwitches = [GameInputSwitchPosition.GameInputSwitchUp, GameInputSwitchPosition.GameInputSwitchDown];
        MockReading readingStub = new()
        {
            Switches = mockSwitches
        };

        using GameInputReading reading = new(readingStub);
        GameInputSwitchPosition[] buffer = new GameInputSwitchPosition[4];

        uint written = reading.GetControllerSwitchState(buffer);

        Assert.AreEqual(2u, written);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchUp, buffer[0]);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchDown, buffer[1]);
        Assert.AreEqual(GameInputSwitchPosition.GameInputSwitchCenter, buffer[2]);
    }

    [TestMethod]
    public void GetKeyStateWithBufferWritesExpectedData()
    {
        GameInputKeyState[] mockKeys =
        [
            new GameInputKeyState { ScanCode = 10, VirtualKey = 65 },
            new GameInputKeyState { ScanCode = 11, VirtualKey = 66 }
        ];
        MockReading readingStub = new()
        {
            Keys = mockKeys
        };

        using GameInputReading reading = new(readingStub);
        GameInputKeyState[] buffer = new GameInputKeyState[4];

        uint written = reading.GetKeyState(buffer);

        Assert.AreEqual(2u, written);
        Assert.AreEqual(10u, buffer[0].ScanCode);
        Assert.AreEqual(65u, buffer[0].VirtualKey);
        Assert.AreEqual(11u, buffer[1].ScanCode);
        Assert.AreEqual(66u, buffer[1].VirtualKey);
        Assert.AreEqual(0u, buffer[2].ScanCode);
    }

    private sealed class MockReading : IGameInputReading
    {
        public float[] Axes { get; set; } = [];
        public byte[] Buttons { get; set; } = [];
        public GameInputSwitchPosition[] Switches { get; set; } = [];
        public GameInputKeyState[] Keys { get; set; } = [];

        public GameInputKind GetInputKind() => throw new NotImplementedException();
        public ulong GetTimestamp() => throw new NotImplementedException();
        public void GetDevice(out IGameInputDevice? device) => throw new NotImplementedException();

        public uint GetControllerAxisCount() => (uint)Axes.Length;
        public uint GetControllerAxisState(uint stateArrayCount, float[] stateArray)
        {
            uint count = Math.Min(stateArrayCount, (uint)Axes.Length);
            for (int i = 0; i < count; i++)
            {
                stateArray[i] = Axes[i];
            }
            return count;
        }

        public uint GetControllerButtonCount() => (uint)Buttons.Length;
        public uint GetControllerButtonState(uint stateArrayCount, byte[] stateArray)
        {
            uint count = Math.Min(stateArrayCount, (uint)Buttons.Length);
            for (int i = 0; i < count; i++)
            {
                stateArray[i] = Buttons[i];
            }
            return count;
        }

        public uint GetControllerSwitchCount() => (uint)Switches.Length;
        public uint GetControllerSwitchState(uint stateArrayCount, GameInputSwitchPosition[] stateArray)
        {
            uint count = Math.Min(stateArrayCount, (uint)Switches.Length);
            for (int i = 0; i < count; i++)
            {
                stateArray[i] = Switches[i];
            }
            return count;
        }

        public uint GetKeyCount() => (uint)Keys.Length;
        public uint GetKeyState(uint stateArrayCount, GameInputKeyState[] stateArray)
        {
            uint count = Math.Min(stateArrayCount, (uint)Keys.Length);
            for (int i = 0; i < count; i++)
            {
                stateArray[i] = Keys[i];
            }
            return count;
        }

        public bool GetMouseState(out GameInputMouseState state) => throw new NotImplementedException();
        public bool GetSensorsState(out GameInputSensorsState state) => throw new NotImplementedException();
        public bool GetArcadeStickState(out GameInputArcadeStickState state) => throw new NotImplementedException();
        public bool GetFlightStickState(out GameInputFlightStickState state) => throw new NotImplementedException();
        public bool GetGamepadState(out GameInputGamepadState state) => throw new NotImplementedException();
        public bool GetRacingWheelState(out GameInputRacingWheelState state) => throw new NotImplementedException();
        public bool GetRawReport(out IGameInputRawDeviceReport? report) => throw new NotImplementedException();
    }
}
