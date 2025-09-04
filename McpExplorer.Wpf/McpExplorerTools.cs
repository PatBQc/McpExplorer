using ModelContextProtocol.Server;
using System.ComponentModel;
using System.IO;

namespace McpExplorer.Wpf;

/// <summary>
/// Définit un ensemble d'outils qui seront exposés par le serveur MCP.
/// L'attribut [McpServerToolType] marque cette classe comme un conteneur d'outils MCP.
/// </summary>
[McpServerToolType]
public static class McpExplorerTools
{
    /// <summary>
    /// Un outil pour transcrire un fichier audio en texte.
    /// L'attribut [McpServerTool] expose cette méthode comme un outil MCP.
    /// L'attribut [Description] fournit une description lisible par l'homme de ce que fait l'outil.
    /// </summary>
    /// <param name="mp3FilePath">Le chemin d'accès au fichier MP3 à transcrire.</param>
    /// <returns>Le texte transcrit.</returns>
    [McpServerTool, Description("Transcribes an audio file to text.")]
    public static string TranscribeAudio(string mp3FilePath)
    {
        // Dans une application réelle, vous appelleriez ici un service comme Whisper Online.
        // Pour cet exemple, nous retournons une transcription factice.
        return $"Transcription of '{mp3FilePath}': This is a test transcription.";
    }

    /// <summary>
    /// Un outil pour optimiser un texte pour un courriel professionnel.
    /// </summary>
    /// <param name="text">Le texte à optimiser.</param>
    /// <returns>Le texte formaté comme un prompt pour un modèle de langage.</returns>
    [McpServerTool, Description("Optimizes text for an email.")]
    public static string OptimizeForEmail(string text)
    {
        // Lit le modèle de prompt depuis le fichier .prompty.
        var promptTemplate = File.ReadAllText("Prompts/OptimizeForEmail.prompty");
        // Remplace le placeholder {{text}} par le texte fourni.
        var prompt = promptTemplate.Replace("{{text}}", text);
        // Dans une application réelle, vous enverriez ce prompt à un modèle de langage
        // pour obtenir le résultat final. Ici, nous retournons simplement le prompt généré.
        return $"--- PROMPT FOR EMAIL ---\n{prompt}";
    }

    /// <summary>
    /// Un outil pour optimiser un texte pour un message Microsoft Teams.
    /// </summary>
    /// <param name="text">Le texte à optimiser.</param>
    /// <returns>Le texte formaté comme un prompt pour un modèle de langage.</returns>
    [McpServerTool, Description("Optimizes text for a Teams message.")]
    public static string OptimizeForTeams(string text)
    {
        // Lit le modèle de prompt depuis le fichier .prompty.
        var promptTemplate = File.ReadAllText("Prompts/OptimizeForTeams.prompty");
        // Remplace le placeholder {{text}} par le texte fourni.
        var prompt = promptTemplate.Replace("{{text}}", text);
        // Dans une application réelle, vous enverriez ce prompt à un modèle de langage.
        return $"--- PROMPT FOR TEAMS ---\n{prompt}";
    }
}
