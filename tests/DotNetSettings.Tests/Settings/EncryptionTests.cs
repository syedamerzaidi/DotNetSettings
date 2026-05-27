using DotNetSettings.Tests.Helpers;
using FluentAssertions;
using System.Text.Json;

namespace DotNetSettings.Tests.Settings;

public class EncryptionTests
{
    [Fact]
    public void Encrypted_property_is_stored_encrypted_in_repository()
    {
        var (manager, repo) = SettingsManagerFactory.Create(withEncryption: true);
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.ApiKey = "super-secret";
        settings.Save();

        var stored = repo.GetPropertiesInGroup("general");
        // The stored value must NOT equal the plain JSON
        stored["ApiKey"].Should().NotBe("\"super-secret\"");
        stored["ApiKey"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Encrypted_property_is_decrypted_on_load()
    {
        var (manager, repo) = SettingsManagerFactory.Create(withEncryption: true);
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.ApiKey = "my-api-key";
        settings.Save();

        // Reload
        var reloaded = (manager, repo).Load<GeneralSettings>();
        reloaded.ApiKey.Should().Be("my-api-key");
    }

    [Fact]
    public void Non_encrypted_property_is_stored_as_plain_json()
    {
        var (manager, repo) = SettingsManagerFactory.Create(withEncryption: true);
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.SiteName = "plain text";
        settings.Save();

        var stored = repo.GetPropertiesInGroup("general");
        JsonSerializer.Deserialize<string>(stored["SiteName"]!).Should().Be("plain text");
    }

    [Fact]
    public void Encrypt_attribute_is_respected()
    {
        var (manager, repo) = SettingsManagerFactory.Create(withEncryption: true);
        var settings = (manager, repo).Load<GeneralSettings>();

        settings.ApiKey = "secret";
        settings.Save();

        // Raw stored payload should not be deserializable as plain JSON string "secret"
        var stored = repo.GetPropertiesInGroup("general");
        var action = () => JsonSerializer.Deserialize<string>(stored["ApiKey"]!);
        action.Should().Throw<Exception>("encrypted payload is not valid JSON string");
    }
}
