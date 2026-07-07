namespace InputWeave.GameInput.Tests;

[TestClass]
public sealed class GameInputRuntimeLoaderTests
{
    [TestMethod]
    public void RuntimeLoaderSelectsOnlyInboxRuntime()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 4, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.SystemGameInput, selected);
    }

    [TestMethod]
    public void RuntimeLoaderSelectsOnlySystemRedistRuntime()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: true, new Version(3, 4, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.SystemGameInputRedist, selected);
    }

    [TestMethod]
    public void RuntimeLoaderSelectsOnlyRegistryRedistRuntime()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.RegistryGameInputRedist, exists: true, new Version(3, 4, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.RegistryGameInputRedist, selected);
    }

    [TestMethod]
    public void RuntimeLoaderPrefersNewerInboxRuntime()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 4, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: true, new Version(3, 3, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.SystemGameInput, selected);
    }

    [TestMethod]
    public void RuntimeLoaderPrefersNewerRedistRuntime()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 3, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: true, new Version(3, 4, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.SystemGameInputRedist, selected);
    }

    [TestMethod]
    public void RuntimeLoaderPrefersRedistRuntimeWhenVersionsMatch()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 4, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: true, new Version(3, 4, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.SystemGameInputRedist, selected);
    }

    [TestMethod]
    public void RuntimeLoaderUsesRegistryRedistWhenSystemRedistIsMissing()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 3, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: false, null),
            Candidate(GameInputRuntimeModuleKind.RegistryGameInputRedist, exists: true, new Version(3, 4, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.RegistryGameInputRedist, selected);
    }

    [TestMethod]
    public void RuntimeLoaderPrefersSystemRedistBeforeRegistryFallback()
    {
        GameInputRuntimeModuleKind selected = Select(
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 3, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: true, new Version(3, 4, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.RegistryGameInputRedist, exists: true, new Version(3, 5, 0, 0)));

        Assert.AreEqual(GameInputRuntimeModuleKind.SystemGameInputRedist, selected);
    }

    [TestMethod]
    public void RuntimeLoaderBuildsFallbackLoadOrderAfterPreferredCandidate()
    {
        IReadOnlyList<GameInputRuntimeCandidate> loadOrder = GameInputRuntimeLoader.BuildCandidateLoadOrder(
        [
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 3, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: true, new Version(3, 4, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.RegistryGameInputRedist, exists: true, new Version(3, 5, 0, 0))
        ]);

        CollectionAssert.AreEqual(
            new[]
            {
                GameInputRuntimeModuleKind.SystemGameInputRedist,
                GameInputRuntimeModuleKind.RegistryGameInputRedist,
                GameInputRuntimeModuleKind.SystemGameInput
            },
            loadOrder.Select(static candidate => candidate.ModuleKind).ToArray());
    }

    [TestMethod]
    public void RuntimeLoaderFallbackLoadOrderStartsWithNewerInboxWhenPreferred()
    {
        IReadOnlyList<GameInputRuntimeCandidate> loadOrder = GameInputRuntimeLoader.BuildCandidateLoadOrder(
        [
            Candidate(GameInputRuntimeModuleKind.SystemGameInput, exists: true, new Version(3, 5, 0, 0)),
            Candidate(GameInputRuntimeModuleKind.SystemGameInputRedist, exists: true, new Version(3, 4, 0, 0))
        ]);

        CollectionAssert.AreEqual(
            new[]
            {
                GameInputRuntimeModuleKind.SystemGameInput,
                GameInputRuntimeModuleKind.SystemGameInputRedist
            },
            loadOrder.Select(static candidate => candidate.ModuleKind).ToArray());
    }

    [TestMethod]
    public void RuntimeProbeReturnsStableDiagnostics()
    {
        bool available = GameInputRuntime.TryProbe(out GameInputRuntimeProbeInfo info);

        Assert.AreEqual(available, info.IsAvailable);
        Assert.AreEqual(GameInputRuntimeLoader.LoaderPolicy, info.LoaderPolicy);
        Assert.IsGreaterThanOrEqualTo(info.Candidates.Count, 3);
        AssertCandidateExists(info.Candidates, GameInputRuntimeModuleKind.SystemGameInput);
        AssertCandidateExists(info.Candidates, GameInputRuntimeModuleKind.SystemGameInputRedist);
        AssertCandidateExists(info.Candidates, GameInputRuntimeModuleKind.RegistryGameInputRedist);
    }

    [TestMethod]
    public void RuntimeInfoIsStableAfterLoad()
    {
        try
        {
            GameInputRuntimeInfo first = GameInputRuntime.GetInfo();
            GameInputRuntimeInfo second = GameInputRuntime.GetInfo();

            Assert.IsTrue(first.IsAvailable);
            Assert.AreEqual(first.LoaderPolicy, second.LoaderPolicy);
            Assert.AreEqual(first.LoadedModuleKind, second.LoadedModuleKind);
            Assert.AreEqual(first.LoadedModulePath, second.LoadedModulePath);
        }
        catch (DllNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境未載入 GameInput runtime：{ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            Assert.Inconclusive($"此測試環境的 GameInput runtime 不含必要進入點：{ex.Message}");
        }
    }

    private static GameInputRuntimeCandidate Candidate(GameInputRuntimeModuleKind moduleKind, bool exists, Version? fileVersion)
    {
        return GameInputRuntimeLoader.CreateTestCandidate(moduleKind, exists, fileVersion);
    }

    private static GameInputRuntimeModuleKind Select(params GameInputRuntimeCandidate[] candidates)
    {
        GameInputRuntimeCandidate? selected = GameInputRuntimeLoader.SelectPreferredCandidate(candidates);

        Assert.IsTrue(selected.HasValue, "應選出可用的 GameInput runtime 候選。");
        return selected.Value.ModuleKind;
    }

    private static void AssertCandidateExists(
        IReadOnlyList<GameInputRuntimeCandidateInfo> candidates,
        GameInputRuntimeModuleKind moduleKind)
    {
        Assert.IsTrue(
            candidates.Any(candidate => candidate.ModuleKind == moduleKind),
            $"探測結果應包含 {moduleKind} 候選。");
    }
}
