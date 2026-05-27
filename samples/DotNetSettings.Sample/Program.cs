using DotNetSettings;
using DotNetSettings.Migrations;
using DotNetSettings.Sample.Settings;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();

// Generate a key once: Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
// Store it in a secret store (Key Vault, user-secrets, environment variable) — never commit it.
var encryptionKey = builder.Configuration["Settings:EncryptionKey"]
    ?? "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="; // demo only

builder.Services.AddDotNetSettings(options =>
{
    options.UseDatabase(
        "Data Source=sample.db",
        b => b.UseSqlite("Data Source=sample.db"));

    options.EnableCaching(ttl: TimeSpan.FromMinutes(5));
    options.UseEncryption(encryptionKey);

    options.RegisterMigrationsFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddSettings<GeneralSettings>();
builder.Services.AddSettings<MailSettings>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetService<SettingsMigrationRunner>();
    if (runner is not null)
        await runner.RunAsync();
}

app.MapGet("/settings/general", (GeneralSettings s) => new
{
    s.SiteName,
    s.SiteActive,
    s.MaxUploadSizeMb,
    s.LaunchedAt,
    ApiKey = "[encrypted]"
});

app.MapPost("/settings/general", (GeneralSettings s, UpdateGeneralRequest req) =>
{
    s.SiteName = req.SiteName;
    s.SiteActive = req.SiteActive;
    s.Save();
    return Results.Ok(new { s.SiteName, s.SiteActive });
});

app.MapPost("/settings/general/lock", (GeneralSettings s) =>
{
    s.Lock(nameof(GeneralSettings.SiteName));
    return Results.Ok(new { Locked = s.GetLockedProperties() });
});

app.MapGet("/settings/mail", (MailSettings s) => new
{
    s.FromAddress,
    s.FromName,
    s.SendWelcomeEmail
});

app.Run();

record UpdateGeneralRequest(string SiteName, bool SiteActive);
