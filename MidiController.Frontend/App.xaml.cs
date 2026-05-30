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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var collection = new ServiceCollection();
        ConfigureServices(collection, config);
        Services = collection.BuildServiceProvider();

        Services.GetRequiredService<MainWindow>().Show();
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
            new MidiLogViewModel($"{backend.BaseUrl}/hubs/midilog", sp.GetRequiredService<ApiClient>()));
        services.AddSingleton<DevicesViewModel>();
        services.AddSingleton<MappingsViewModel>();
        services.AddSingleton<TemplatesViewModel>();

        // Hauptfenster
        services.AddSingleton<MainWindow>();
    }
}

