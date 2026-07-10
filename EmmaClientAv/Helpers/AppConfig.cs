
using System;
using System.IO;
using System.Text.Json;

namespace EmmaClientAv.Helpers;

// Windows: C:\Users\NomeUtente\AppData\Roaming\EmmaClientAv\config.json
// Linux:: /home/NomeUtente/.config/EmmaClientAv/config.json
// macOS: /Users/NomeUtente/Library/Application Support/EmmaClientAv/config.json
public class AppConfig
{
    public string ServerUrl { get; set; } = "https://emma-server-uda8.onrender.com";
}

public static class ConfigManager
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "EmmaClient" // Nome della tua cartella dell'app
    );
    
    //Linux Mint /home/sly/.config/EmmaClient
    private static readonly string FilePath = Path.Combine(FolderPath, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                // Se non esiste, crea una configurazione di default e salvala
                var defaultConfig = new AppConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            // In caso di errore di lettura, ritorna il default per non crashare
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        try
        {
            // Crea la cartella se non esiste (fondamentale su Linux/macOS)
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            // Gestisci l'eccezione o loggala a seconda delle esigenze
            Console.WriteLine($"Impossibile salvare la configurazione: {ex.Message}");
        }
    }
}