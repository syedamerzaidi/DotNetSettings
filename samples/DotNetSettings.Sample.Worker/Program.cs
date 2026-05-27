using DotNetSettings;
using DotNetSettings.Migrations;
using DotNetSettings.Sample.Worker;
using DotNetSettings.Sample.Worker.Settings;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDotNetSettings(options =>
{
    options.UseDatabase(
        "Data Source=worker.db",
        b => b.UseSqlite("Data Source=worker.db"));

    options.RegisterMigrationsFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddSettings<WorkerSettings>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetService<SettingsMigrationRunner>();
    if (runner is not null)
        await runner.RunAsync();
}

await host.RunAsync();
