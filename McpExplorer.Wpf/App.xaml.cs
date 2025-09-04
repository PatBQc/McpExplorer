using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace McpExplorer.Wpf;

/// <summary>
/// Point d'entrée principal de l'application WPF.
/// Gère le cycle de vie de l'application et du serveur MCP.
/// </summary>
public partial class App : Application
{
    // L'hôte .NET générique qui gérera notre serveur MCP.
    // L'utilisation de IHost nous permet de bénéficier de l'injection de dépendances,
    // de la configuration et de la journalisation de manière structurée.
    private IHost? _host;

    /// <summary>
    /// Se déclenche au démarrage de l'application.
    /// C'est ici que nous configurons et démarrons le serveur MCP.
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        var builder = Host.CreateApplicationBuilder(e.Args);

        // Configure l'application pour lire le fichier appsettings.json
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        // Enregistre les services, y compris la fenêtre principale pour l'injection de dépendances.
        builder.Services.AddSingleton<MainWindow>();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
        
        _host = builder.Build();
        await _host.StartAsync();

        // Récupère la fenêtre principale depuis le conteneur de services
        // et l'affiche. Le conteneur injectera automatiquement IConfiguration.
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    /// <summary>
    /// Se déclenche à la fermeture de l'application.
    /// Nous nous assurons ici que le serveur MCP est arrêté proprement.
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            // Arrête l'hôte de manière asynchrone.
            await _host.StopAsync();
            // Libère les ressources utilisées par l'hôte.
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
