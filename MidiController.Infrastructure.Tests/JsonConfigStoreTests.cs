using MidiController.Domain.Enums;
using MidiController.Domain.Models;
using MidiController.Infrastructure.Config;

namespace MidiController.Infrastructure.Tests;

public sealed class JsonConfigStoreTests : IDisposable
{
    private readonly string _basePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly JsonConfigStore _store;

    public JsonConfigStoreTests()
    {
        _store = new JsonConfigStore(_basePath);
    }

    public void Dispose() => Directory.Delete(_basePath, recursive: true);

    // ── Profiles ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoad_Profile_RoundTrips()
    {
        var profile = MakeProfile("test-profile");
        await _store.SaveProfileAsync(profile);

        var loaded = await _store.LoadProfileAsync("test-profile");

        Assert.NotNull(loaded);
        Assert.Equal("test-profile", loaded.ProfileId);
        Assert.Single(loaded.Devices);
        Assert.Equal("dev1", loaded.Devices[0].PhysicalDeviceId);
    }

    [Fact]
    public async Task ListProfiles_ReturnsAllSavedIds()
    {
        await _store.SaveProfileAsync(MakeProfile("p1"));
        await _store.SaveProfileAsync(MakeProfile("p2"));

        var ids = (await _store.ListProfileIdsAsync()).ToList();

        Assert.Contains("p1", ids);
        Assert.Contains("p2", ids);
    }

    [Fact]
    public async Task LoadProfile_ReturnsNull_WhenNotFound()
    {
        var result = await _store.LoadProfileAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteProfile_RemovesFile()
    {
        await _store.SaveProfileAsync(MakeProfile("del-me"));
        await _store.DeleteProfileAsync("del-me");

        var result = await _store.LoadProfileAsync("del-me");
        Assert.Null(result);
    }

    // ── ConditionBlock-Templates ──────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoad_ConditionTemplate_RoundTrips()
    {
        var template = new ConditionBlock(
            "myCondition",
            [new Condition(ValueSource.MidiData1, ">=", ValueSource.Fixed, 64)]);

        await _store.SaveConditionBlockTemplateAsync(template);
        var loaded = await _store.LoadConditionBlockTemplateAsync("myCondition");

        Assert.NotNull(loaded);
        Assert.Equal("myCondition", loaded.TemplateName);
        Assert.Single(loaded.Conditions);
        Assert.Equal(">=", loaded.Conditions[0].Op);
    }

    [Fact]
    public async Task LoadConditionTemplate_ReturnsNull_WhenNotFound()
    {
        var result = await _store.LoadConditionBlockTemplateAsync("ghost");
        Assert.Null(result);
    }

    // ── ActionBlock-Templates ─────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoad_ActionTemplate_RoundTrips()
    {
        var template = new ActionBlock(
            TemplateName:      "myAction",
            KeyCombination:    ["ctrl", "c"],
            XSource:           ValueSource.Fixed, XFixed: 1,
            YSource:           ValueSource.Fixed, YFixed: 0,
            ZSource:           ValueSource.Fixed, ZFixed: 50,
            StateAssignments:  []);

        await _store.SaveActionBlockTemplateAsync(template);
        var loaded = await _store.LoadActionBlockTemplateAsync("myAction");

        Assert.NotNull(loaded);
        Assert.Equal("myAction", loaded.TemplateName);
        Assert.Equal(["ctrl", "c"], loaded.KeyCombination);
    }

    [Fact]
    public async Task DeleteTemplate_RemovesBothKinds()
    {
        var cond = new ConditionBlock("shared", []);
        await _store.SaveConditionBlockTemplateAsync(cond);

        await _store.DeleteTemplateAsync("shared");

        Assert.Null(await _store.LoadConditionBlockTemplateAsync("shared"));
    }

    [Fact]
    public async Task ListTemplateNames_IncludesAllSaved()
    {
        await _store.SaveConditionBlockTemplateAsync(new ConditionBlock("condA", []));
        await _store.SaveActionBlockTemplateAsync(new ActionBlock(
            "actB", [], ValueSource.Fixed, 1, ValueSource.Fixed, 0, ValueSource.Fixed, 0, []));

        var names = (await _store.ListTemplateNamesAsync()).ToList();

        Assert.Contains("condA", names);
        Assert.Contains("actB", names);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────

    private static Profile MakeProfile(string id) => new(
        ProfileId: id,
        Devices:   [new DeviceMapping("dev1", [])],
        Triggers:  []);
}
