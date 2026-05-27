using DotNetSettings.Casts;
using DotNetSettings.Tests.Helpers;
using FluentAssertions;

namespace DotNetSettings.Tests.Settings;

public class CastingTests
{
    [Fact]
    public void DateTime_property_round_trips_correctly()
    {
        var (manager, repo) = SettingsManagerFactory.Create(
            globalCasts: new() { [typeof(DateTime)] = new DateTimeSettingsCast() });
        var settings = (manager, repo).Load<CastSettings>();

        var expected = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        settings.CreatedAt = expected;
        settings.Save();

        var reloaded = (manager, repo).Load<CastSettings>();
        reloaded.CreatedAt.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DateTimeOffset_property_round_trips_correctly()
    {
        var (manager, repo) = SettingsManagerFactory.Create(
            globalCasts: new() { [typeof(DateTimeOffset)] = new DateTimeOffsetSettingsCast() });
        var settings = (manager, repo).Load<CastSettings>();

        var expected = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.FromHours(5));
        settings.UpdatedAt = expected;
        settings.Save();

        var reloaded = (manager, repo).Load<CastSettings>();
        reloaded.UpdatedAt.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Enum_property_is_stored_as_string_name()
    {
        var cast = new EnumSettingsCast();

        var payload = cast.Set(Status.Pending);
        payload.Should().Be("\"Pending\"");

        var value = cast.Get(payload, typeof(Status));
        value.Should().Be(Status.Pending);
    }

    [Fact]
    public void Custom_cast_is_applied_on_get_and_set()
    {
        var (manager, repo) = SettingsManagerFactory.Create();
        var settings = (manager, repo).Load<CastSettings>();

        settings.Custom = new CustomValue("hello");
        settings.Save();

        var reloaded = (manager, repo).Load<CastSettings>();
        reloaded.Custom.Should().Be(new CustomValue("hello"));
    }

    [Fact]
    public void Global_cast_is_resolved_for_registered_type()
    {
        var dtCast = new DateTimeSettingsCast();
        var (manager, repo) = SettingsManagerFactory.Create(
            globalCasts: new() { [typeof(DateTime)] = dtCast });

        var settings = (manager, repo).Load<CastSettings>();
        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        settings.CreatedAt = dt;
        settings.Save();

        var raw = repo.GetPropertyPayload("cast", "CreatedAt");
        raw.Should().Contain("2025");
    }
}
