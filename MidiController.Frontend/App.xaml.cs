using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MidiControllerFrontend.Services;
using MidiControllerFrontend.ViewModels;
using System.IO;
using System.Windows;

namespace MidiControllerFrontend;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private BackendHostService? _backendHost;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // App läuft weiter, wenn das Hauptfenster geschlossen wird (→ Tray)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Backend in-process starten (blockiert kurz bis Kestrel bereit)
        _backendHost = new BackendHostService();
        _backendHost.StartAsync().GetAwaiter().GetResult();

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var collection = new ServiceCollection();
        ConfigureServices(collection, config);
        Services = collection.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();

        // TrayService erzeugen – braucht MainWindow und MainViewModel
        var trayService = new TrayService(
            Services.GetRequiredService<MainViewModel>(),
            mainWindow);

        // Beim App-Shutdown Backend und TrayService aufräumen
        Exit += async (_, _) =>
        {
            trayService.Dispose();
            if (_backendHost is not null)
                await _backendHost.DisposeAsync();
        };

        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Options
        var backend = config.GetSection("Backend").Get<BackendOptions>() ?? new BackendOptions();
        var ui      = config.GetSection("UI").Get<UiOptions>() ?? new UiOptions();
        services.AddSingleton(backend);
        services.AddSingleton(ui);

        // HTTP-Client
        services.AddHttpClient<ApiClient>(c => c.BaseAddress = new Uri(backend.BaseUrl));

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<StatusViewModel>(sp =>
            new StatusViewModel($"{backend.BaseUrl}/hubs/status", sp.GetRequiredService<ApiClient>()));
        services.AddSingleton<MidiLogViewModel>(sp =>
            new MidiLogViewModel($"{backend.BaseUrl}/hubs/midilog",
                sp.GetRequiredService<ApiClient>(),
                sp.GetRequiredService<MainViewModel>()));
        services.AddSingleton<DevicesViewModel>();
        services.AddSingleton<MappingsViewModel>(sp =>
            new MappingsViewModel(
                sp.GetRequiredService<ApiClient>(),
                new SignalRClientService($"{backend.BaseUrl}/hubs/midilog")));
        services.AddSingleton<TemplatesViewModel>();
        services.AddSingleton<KeyTestViewModel>();

        // Hauptfenster
        services.AddSingleton<MainWindow>();
    }
}

