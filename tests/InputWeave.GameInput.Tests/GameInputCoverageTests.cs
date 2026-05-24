using System.Reflection;
using InputWeave.GameInput.Interop;

namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputCoverageTests
{
    [TestMethod]
    public void ProjectVersionUsesNuGetZeroZeroOne()
    {
        string project = File.ReadAllText(FindRepoFile("src/InputWeave.GameInput/InputWeave.GameInput.csproj"));

        StringAssert.Contains(project, "<Version>0.0.1</Version>");
        Assert.IsFalse(project.Contains("v.0.0.1", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CoverageReportStatesNoGapsForVersionZeroZeroOne()
    {
        string report = File.ReadAllText(FindRepoFile("docs/gameinput-api-coverage.md"));

        StringAssert.Contains(report, "InputWeave.GameInput v0.0.1");
        StringAssert.Contains(report, "Microsoft.GameInput 3.4.218");
        StringAssert.Contains(report, "缺口：0");
        Assert.IsFalse(report.Contains("v.0.0.1", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GeneratedInteropSurfaceHasWrapperCoverageReport()
    {
        string manifest = File.ReadAllText(FindRepoFile("src/InputWeave.GameInput/Interop/Generated/gameinput-abi-manifest.json"));
        string report = File.ReadAllText(FindRepoFile("docs/gameinput-api-coverage.md"));

        StringAssert.Contains(manifest, "\"apiVersion\": 3");
        StringAssert.Contains(report, "enum：27 / 27");
        StringAssert.Contains(report, "struct：32 / 32");
        StringAssert.Contains(report, "callback delegate：4 / 4");
        StringAssert.Contains(report, "COM interface：7 / 7");
        StringAssert.Contains(report, "HRESULT：10 / 10");
    }

    [TestMethod]
    public void ForceFeedbackBuilderProducesExpectedKinds()
    {
        GameInputForceFeedbackEnvelope envelope = GameInputForceFeedback.Envelope();
        GameInputForceFeedbackMagnitude magnitude = GameInputForceFeedback.Magnitude(normal: 0.5f);
        GameInputForceFeedbackParams constant = GameInputForceFeedback.Constant(magnitude, envelope);
        GameInputForceFeedbackParams sine = GameInputForceFeedback.Periodic(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave, magnitude, envelope, frequency: 10);

        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackConstant, constant.Kind);
        Assert.AreEqual(0.5f, constant.Constant.Magnitude.Normal);
        Assert.AreEqual(GameInputForceFeedbackEffectKind.GameInputForceFeedbackSineWave, sine.Kind);
        Assert.AreEqual(10, sine.SineWave.Frequency);
    }

    [TestMethod]
    public void PublicDisposableTypesManageNativeLifetime()
    {
        Type[] disposableTypes =
        [
                typeof(GameInputClient),
                typeof(GameInputDevice),
                typeof(GameInputDispatcher),
                typeof(GameInputMapper),
                typeof(GameInputRawDeviceReport),
                typeof(GameInputForceFeedbackEffect),
                typeof(GameInputCallbackRegistration),
                typeof(GameInputReading),
                typeof(GameInputDeviceManager),
                typeof(GameInputDispatcherWaitHandle)
            ];

        foreach (Type type in disposableTypes)
        {
            Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(type), $"{type.Name} 應明確管理 native lifetime。");
            Assert.IsNotNull(type.GetMethod(nameof(IDisposable.Dispose), BindingFlags.Instance | BindingFlags.Public), $"{type.Name} 應有公開 Dispose。");
        }
    }

    private static string FindRepoFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("找不到 repo 檔案。", relativePath);
    }
}
