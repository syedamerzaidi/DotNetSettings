using DotNetSettings.Casts;
using DotNetSettings.Encryption;
using DotNetSettings.Repositories;

namespace DotNetSettings.Tests.Helpers;

internal static class SettingsManagerFactory
{
    // Fixed 32-byte key used only in tests.
    private static readonly byte[] TestKey =
        Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");

    public static (SettingsManager manager, FakeSettingsRepository repo) Create(
        Dictionary<Type, ISettingsCast>? globalCasts = null,
        bool withEncryption = false)
    {
        var repo = new FakeSettingsRepository();
        SettingsEncryptor? encryptor = withEncryption ? new SettingsEncryptor(TestKey) : null;

        var manager = new SettingsManager(
            repo,
            globalCasts: globalCasts,
            encryptor: encryptor);

        return (manager, repo);
    }

    public static T Load<T>(this (SettingsManager manager, FakeSettingsRepository repo) ctx)
        where T : DotNetSettings.Settings
    {
        var instance = Activator.CreateInstance<T>();
        instance.Repository = ctx.repo;
        instance.Manager = ctx.manager;
        ctx.manager.Load(instance);
        return instance;
    }
}
