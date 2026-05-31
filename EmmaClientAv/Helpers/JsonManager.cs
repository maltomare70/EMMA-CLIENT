  
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

  namespace EmmaClientAv.Helpers;
  
  public static class JsonManager
  {
         private static readonly JsonSerializerOptions _options = new()
          {
              WriteIndented = true,
              Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Preserva lettere accentate
          };

         public static async Task SaveFileLocally(Stream streamSorgente, string filePath)
         {
             string? directory = Path.GetDirectoryName(filePath);
             if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
             {
                 Directory.CreateDirectory(directory);
             }

             // Apriamo lo stream di scrittura del file di destinazione sul disco
             await using var destinazioneStream = new FileStream(
                 filePath, 
                 FileMode.Create, 
                 FileAccess.Write, 
                 FileShare.None, 
                 bufferSize: 4096, 
                 useAsync: true
             );

             // Copiamo i byte direttamente dallo stream sorgente a quello di destinazione sul disco.
             await streamSorgente.CopyToAsync(destinazioneStream);
         }
  }