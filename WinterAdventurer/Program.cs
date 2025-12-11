// <copyright file="Program.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using PdfSharp.Fonts;
using Serilog;
using Serilog.Events;
using WinterAdventurer.Components;
using WinterAdventurer.Data;
using WinterAdventurer.Library;
using WinterAdventurer.Library.Services;
using WinterAdventurer.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/winteradventurer-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("Starting WinterAdventurer application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for logging
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents(options =>
        {
            options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(3);
        })
        .AddHubOptions(options =>
        {
            options.ClientTimeoutInterval = TimeSpan.FromMinutes(3);
            options.HandshakeTimeout = TimeSpan.FromSeconds(30);
            options.MaximumReceiveMessageSize = 128 * 1024; // 128KB for large PDFs
        });

    // Configure circuit options for idle timeout handling
    // Single-user workflow: keep circuits alive for extended periods
    builder.Services.AddServerSideBlazor()
        .AddCircuitOptions(options =>
        {
            // Keep disconnected circuits for 5 hours (single-user scenario)
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromHours(5);

            // Only one user at a time, so low retention count is fine
            options.DisconnectedCircuitMaxRetained = 10;
        });
    builder.Services.AddMudServices();

    // Configure database path for portable deployment
    // Use user's LocalApplicationData folder to ensure writable location for single-file executables
    var dataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinterAdventurer");
    Directory.CreateDirectory(dataDirectory);

    var defaultConnectionString = $"Data Source={Path.Combine(dataDirectory, "winteradventurer.db")}";

    // Add database context
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? defaultConnectionString));

    builder.Services.AddSingleton<DbSeeder>();

    // Add location service
    builder.Services.AddScoped<ILocationService, LocationService>();

    // Add tag service
    builder.Services.AddScoped<ITagService, TagService>();

    // Add timeslot validation service
    builder.Services.AddScoped<ITimeslotValidationService, TimeslotValidationService>();

    // Add theme service
    builder.Services.AddScoped<ThemeService>();

    // Add tour service
    builder.Services.AddScoped<TourService>();

    // Add Excel utilities service
    builder.Services.AddScoped<ExcelUtilities>();

    // Configure PDF font resolver (must be set once at startup)
    GlobalFontSettings.FontResolver = new CustomFontResolver();

    var app = builder.Build();

    // Ensure database is created and apply migrations
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        dbContext.Database.Migrate();
    }

    // Seed the database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Get the DbSeeder instance from the container
        var seeder = services.GetRequiredService<DbSeeder>();

        try
        {
            await context.Database.MigrateAsync(); // Ensure DB is updated
            await seeder.SeedDefaultDataAsync(context); // Call the instance method
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);

        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    Log.Information("Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
