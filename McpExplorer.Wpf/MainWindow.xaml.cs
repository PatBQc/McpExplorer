using Anthropic.SDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Win32;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace McpExplorer.Wpf;

/// <summary>
/// Logique d'interaction pour la fenêtre principale de l'application.
/// Contient la logique du client MCP pour interagir avec le serveur in-process.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IConfiguration _configuration;
    private IMcpClient? _mcpClient;

    public MainWindow(IConfiguration configuration)
    {
        _configuration = configuration;
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// Initialise et se connecte au serveur MCP lorsque la fenêtre est chargée.
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Configure le transport client pour Stdio.
        // Le client va démarrer un processus `dotnet run` pour lancer le serveur.
        // C'est ainsi que la communication in-process est établie.
        var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "McpExplorer.Wpf.Client",
            Command = "dotnet",
            // Assurez-vous que le chemin d'accès au projet est correct.
            Arguments = ["run", "--project", "McpExplorer.Wpf.csproj"],
        });

        // Crée et connecte le client MCP de manière asynchrone.
        _mcpClient = await McpClientFactory.CreateAsync(clientTransport);
    }

    /// <summary>
    /// Gère le clic sur le bouton de sélection de fichier.
    /// Appelle l'outil de transcription audio.
    /// </summary>
    private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SelectedFileText.Text = openFileDialog.FileName;
            if (_mcpClient is not null)
            {
                // Appelle l'outil 'TranscribeAudio' sur le serveur.
                var result = await _mcpClient.CallToolAsync(
                    "TranscribeAudio",
                    new Dictionary<string, object?>() { ["mp3FilePath"] = openFileDialog.FileName },
                    cancellationToken: CancellationToken.None);

                // Récupère le résultat et l'affiche.
                if (result.Content.FirstOrDefault(c => c.Type == "text") is TextContentBlock textContent)
                {
                    TranscriptionText.Text = textContent.Text;
                }
            }
        }
    }

    /// <summary>
    /// Gère le clic sur le bouton d'optimisation pour courriel.
    /// </summary>
    private async void OptimizeEmailButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mcpClient is not null && !string.IsNullOrEmpty(TranscriptionText.Text))
        {
            // Appelle l'outil 'OptimizeForEmail'.
            var result = await _mcpClient.CallToolAsync(
                "OptimizeForEmail",
                new Dictionary<string, object?>() { ["text"] = TranscriptionText.Text },
                cancellationToken: CancellationToken.None);

            // Affiche le résultat.
            if (result.Content.FirstOrDefault(c => c.Type == "text") is TextContentBlock textContent)
            {
                OptimizedText.Text = textContent.Text;
            }
        }
    }

    /// <summary>
    /// Gère le clic sur le bouton d'optimisation pour Teams.
    /// </summary>
    private async void OptimizeTeamsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mcpClient is not null && !string.IsNullOrEmpty(TranscriptionText.Text))
        {
            // Appelle l'outil 'OptimizeForTeams'.
            var result = await _mcpClient.CallToolAsync(
                "OptimizeForTeams",
                new Dictionary<string, object?>() { ["text"] = TranscriptionText.Text },
                cancellationToken: CancellationToken.None);

            // Affiche le résultat.
            if (result.Content.FirstOrDefault(c => c.Type == "text") is TextContentBlock textContent)
            {
                OptimizedText.Text = textContent.Text;
            }
        }
    }

    /// <summary>
    /// Gère le clic sur le bouton d'optimisation basée sur le contexte.
    /// C'est ici que la logique d'orchestration côté client est implémentée.
    /// </summary>
    private async void OptimizeContextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mcpClient is null || string.IsNullOrEmpty(TranscriptionText.Text))
        {
            return;
        }

        try
        {
            // 1. Découverte : Lister tous les outils disponibles sur le serveur.
            var allTools = await _mcpClient.ListToolsAsync();

            // 2. Filtrage : Ne conserver que les outils d'optimisation.
            var optimizationTools = allTools.Where(t => t.Name.Contains("Optimize")).ToList();

            if (optimizationTools.Count == 0)
            {
                OptimizedText.Text = "No optimization tools found on the server.";
                return;
            }

            // 3. Analyse et Sélection : Choisir le meilleur outil en fonction du contenu.
            // C'est une simulation simple de ce qu'un LLM ferait.
            string selectedToolName;
            var transcript = TranscriptionText.Text;
            string[] emailKeywords = ["sincerely", "regards", "hello", "dear"];

            if (transcript.Length > 100 || emailKeywords.Any(kw => transcript.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            {
                // Le texte est long ou contient des mots-clés de courriel -> choisir l'outil pour courriel.
                selectedToolName = optimizationTools.FirstOrDefault(t => t.Name.Contains("Email"))?.Name ?? optimizationTools.First().Name;
            }
            else
            {
                // Le texte est court -> choisir l'outil pour Teams.
                selectedToolName = optimizationTools.FirstOrDefault(t => t.Name.Contains("Teams"))?.Name ?? optimizationTools.First().Name;
            }

            OptimizedText.Text = $"Decision: Selected tool '{selectedToolName}' based on content analysis.";

            // 4. Exécution : Appeler l'outil sélectionné dynamiquement.
            var result = await _mcpClient.CallToolAsync(
                selectedToolName,
                new Dictionary<string, object?>() { ["text"] = transcript },
                cancellationToken: CancellationToken.None);

            // 5. Affichage : Montrer le résultat.
            if (result.Content.FirstOrDefault(c => c.Type == "text") is TextContentBlock textContent)
            {
                // Ajoute le résultat à la suite de la décision pour plus de clarté.
                OptimizedText.Text += $"\n\n--- RESULT ---\n{textContent.Text}";
            }
        }
        catch (Exception ex)
        {
            OptimizedText.Text = $"An error occurred: {ex.Message}";
        }
    }

    private async void SelectToolWithAIButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mcpClient is null || string.IsNullOrEmpty(TranscriptionText.Text))
        {
            return;
        }

        try
        {
            OptimizedText.Text = "Asking AI to select the best tool...";

            // 1. Construire le Kernel de Semantic Kernel en fonction de la configuration
            var kernelBuilder = Kernel.CreateBuilder();
            var provider = _configuration["LLM:Provider"];

            switch (provider)
            {
                case "OpenAI":
                    kernelBuilder.AddOpenAIChatCompletion(
                        modelId: _configuration["LLM:OpenAI:Model"]!,
                        apiKey: _configuration["LLM:OpenAI:ApiKey"]!);
                    break;
                case "AzureOpenAI":
                    kernelBuilder.AddAzureOpenAIChatCompletion(
                        deploymentName: _configuration["LLM:AzureOpenAI:DeploymentName"]!,
                        endpoint: _configuration["LLM:AzureOpenAI:Endpoint"]!,
                        apiKey: _configuration["LLM:AzureOpenAI:ApiKey"]!);
                    break;
                default:
                    throw new InvalidOperationException($"Provider '{provider}' is not supported.");
            }
            
            var kernel = kernelBuilder.Build();

            // 2. Découvrir les outils MCP
            var tools = await _mcpClient.ListToolsAsync();
            var toolList = tools
                .Select(t => new { t.Name, t.Description })
                .ToList();

            // 3. Charger et invoquer le prompty de sélection
#pragma warning disable SKEXP0040 // Suppress experimental feature warning
            var function = kernel.CreateFunctionFromPromptyFile("Prompts/MCPToolSelection.prompty");
#pragma warning restore SKEXP0040
            var result = await function.InvokeAsync(kernel, new()
            {
                { "user_task", TranscriptionText.Text },
                { "tools", JsonSerializer.Serialize(toolList) }
            });

            // 4. Désérialiser la décision de l'IA
            var toolSelect = JsonSerializer.Deserialize<ToolSelectResult>(result.GetValue<string>()!);
            if (toolSelect is null || string.IsNullOrEmpty(toolSelect.tool_name))
            {
                throw new InvalidOperationException("AI failed to select a tool.");
            }

            OptimizedText.Text = $"AI selected tool: '{toolSelect.tool_name}'.\nExecuting...";

            // 5. Exécuter l'outil choisi via MCP
            var execResult = await _mcpClient.CallToolAsync(toolSelect.tool_name, new Dictionary<string, object?> { { "text", TranscriptionText.Text } });

            if (execResult.Content.FirstOrDefault(c => c.Type == "text") is TextContentBlock textContent)
            {
                OptimizedText.Text += $"\n\n--- RESULT ---\n{textContent.Text}";
            }
        }
        catch (Exception ex)
        {
            OptimizedText.Text = $"An error occurred: {ex.Message}";
        }
    }

    public class ToolSelectResult
    {
        public string? tool_name { get; set; }
    }
}
