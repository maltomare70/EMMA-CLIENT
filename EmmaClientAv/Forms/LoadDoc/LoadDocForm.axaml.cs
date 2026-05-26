using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using EmmaClientAv.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace EmmaClientAv.Forms.LoadDoc;

public enum TipoDoc
{
    Ordine = 0,
    DDT=1,
    Fattura=2
}
public partial class LoadDocForm : Window
{
    public ObservableCollection<ArticoloBolla> ListaArticoli { get; set; }

    private string Titolo = "";
    
    private TipoDoc _tipoDoc;
    
    public LoadDocForm()
    {
        _tipoDoc = TipoDoc.DDT;

        InitializeComponent();
        
        if (_tipoDoc == TipoDoc.Ordine) Title = "Caricamento Ordine";
        else if (_tipoDoc == TipoDoc.DDT) Title = "Caricamento DDT";
        else if (_tipoDoc == TipoDoc.Fattura) Title = "Caricamento Fattura";
        else Title = "";
        
        ListaArticoli = new ObservableCollection<ArticoloBolla>();
        
        // Colleghiamo la lista al DataGrid
        DataGridArticoli.ItemsSource = ListaArticoli;
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await OpenFilePickerAsync();
        }
        catch (Exception ex)
        {   
            Console.WriteLine(ex);
        }
        
    }
    
    private async Task OpenFilePickerAsync()
    {
        // 1. Recupera lo StorageProvider dalla finestra corrente
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // 2. Configura le opzioni del selettore
        var options = new FilePickerOpenOptions
        {
            Title = "Seleziona un File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                // Filtro per PDF
                new FilePickerFileType("Documenti (*.pdf)")
                {
                    Patterns = new[] { "*.pdf"},
                },
            }
        };

        // 3. Mostra la finestra di dialogo
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        // 4. Gestisci il file selezionato
        if (files.Count > 0)
        {
            // Ottieni il file singolo
            var selectedFile = files[0];

            // Se hai bisogno del percorso assoluto come stringa (solo su Desktop):
            string absolutePath = selectedFile.Path.LocalPath;
            
            Console.WriteLine($"File selezionato: {selectedFile.Name}");
            
            LblNomeFile.Text = $"Elaborazione in corso: {selectedFile.Name}...";
            LblNomeFile.Foreground = Avalonia.Media.Brushes.Orange;
            
            DatiBolla datiBolla = await InviaFileAllApiAsync(selectedFile);

            if (datiBolla != null && datiBolla.Articoli != null)
            {
                // Svuotiamo la collezione corrente sulla UI
                ListaArticoli.Clear();

                // Sfruttiamo il costruttore di ObservableCollection per convertire la List<> dell'API
                // ed inserire gli elementi uno ad uno in modo che la UI si aggiorni istantaneamente
                foreach (var articolo in datiBolla.Articoli)
                {
                    ListaArticoli.Add(articolo);
                }
            }
            
            // Opzionale: Ripristina lo stile del testo (se vuoi togliere l'italico/grigio iniziale)
            LblNomeFile.FontStyle = Avalonia.Media.FontStyle.Normal;
            LblNomeFile.Foreground = Avalonia.Media.Brushes.Black;
        }
    }
    

    private async Task<DatiBolla?> InviaFileAllApiAsync(IStorageFile file)
    {
        
        string urlApi = "http://localhost:8000/api/doc/ddt";

        using var client = new HttpClient();
        using var content = new MultipartFormDataContent();

        try
        {
            // 1. Apriamo lo stream del file in lettura
            await using var fileStream = await file.OpenReadAsync();
            using var streamContent = new StreamContent(fileStream);

            // 2. Impostiamo l'header del tipo di contenuto (opzionale, ma consigliato)
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // 3. Il nome del parametro ("file") DEVE corrispondere esattamente 
            // al nome della variabile nell'API Python: `file: UploadFile`
            content.Add(streamContent, "file", file.Name);

            // 4. Eseguiamo la chiamata POST in modo asincrono
            HttpResponseMessage response = await client.PostAsync(urlApi, content);

            // 5. Verifichiamo l'esito
            if (response.IsSuccessStatusCode)
            {
                //string jsonRisposta = await response.Content.ReadAsStringAsync();
                
                await using var responseStream = await response.Content.ReadAsStreamAsync();
            
                // Opzione A: Se il server restituisce l'oggetto DatiBolla alla radice:
                var datiBolla = await JsonSerializer.DeserializeAsync<DatiBolla>(responseStream);
                return datiBolla;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Errore API: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore durante l'invio: {ex.Message}");
        }
        
        return null;
    }

    private void BtnAggiungi_Click(object? sender, RoutedEventArgs e)
    {
        ListaArticoli.Add(new ArticoloBolla
        {
            Codice = "NEW",
            Descrizione = "Nuovo Articolo (Fai doppio clic per modificare)",
            Quantita = 0,
            UnitaMisura = "PZ",
            Totale = 0
        });
    }

    private void BtnElimina_Click(object? sender, RoutedEventArgs e)
    {
        if (DataGridArticoli.SelectedItem is ArticoloBolla articoloSelezionato)
        {
            ListaArticoli.Remove(articoloSelezionato);
        }
    }
}