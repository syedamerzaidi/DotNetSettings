using DotNetSettings;
using DotNetSettings.Migrations;
using DotNetSettings.Sample.ConsoleApp.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddDotNetSettings(options =>
        {
            options.UseDatabase(
                "Data Source=console.db",
                b => b.UseSqlite("Data Source=console.db"));

            options.RegisterMigrationsFromAssembly(typeof(Program).Assembly);
        });

        services.AddSettings<AppSettings>();
    })
    .Build();

// Run outstanding migrations before using any settings
var runner = host.Services.GetService<SettingsMigrationRunner>();
if (runner is not null)
    await runner.RunAsync();

// Resolve and display settings
var settings = host.Services.GetRequiredService<AppSettings>();

Console.WriteLine($"App      : {settings.AppName}");
Console.WriteLine($"Version  : {settings.Version}");
Console.WriteLine($"Retries  : {settings.MaxRetries}");
Console.WriteLine($"Verbose  : {settings.VerboseLogging}");

// Modify and persist a value
settings.AppName = $"{settings.AppName} (updated)";
settings.MaxRetries = 5;
settings.Save();
Console.WriteLine($"\nSaved — new AppName: {settings.AppName}");

// Lock a setting so it cannot be overwritten
settings.Lock(nameof(AppSettings.Version));
Console.WriteLine($"Locked   : {string.Join(", ", settings.GetLockedProperties())}");

// Test that locking works
settings.Version = "9.9.9";
settings.Save();
settings.Refresh();
Console.WriteLine($"Version after save-with-lock: {settings.Version}");
