using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EmmaClientAv.Helpers;
using EmmaClientAv.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;


namespace EmmaClientAv.Forms.LoadDoc;

public partial class LoadDdtForm : Window
{
    private ObservableCollection<ArticoloBolla> ListaArticoli { get; set; }
    
    public LoadDdtForm()
    {

        InitializeComponent();
        
        ListaArticoli = new ObservableCollection<ArticoloBolla>();
        
        // Colleghiamo la lista al DataGrid
        DataGridArticoli.ItemsSource = ListaArticoli;

        CbTipoDocumento.Items.Add("1. Ordine");
        CbTipoDocumento.Items.Add("2. DDT");
        CbTipoDocumento.Items.Add("3. Fattura Accompagnatoria");
        CbTipoDocumento.Items.Add("4. Fattura");
        CbTipoDocumento.Items.Add("5. Nota di Accredito");
    }

    
    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    { 
        await OpenFilePickerAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    private void CleanUI()
    {
        TxtFornitore.Text = string.Empty;
        TxtNumeroDocumento.Text = string.Empty;
        DpDataDocumento.SelectedDate = null;
        NumImponibile.Value = 0;
        NumTotale.Value = 0;
        ListaArticoli.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
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
            if (topLevel is not Window mainWindow) return;
            
            try
            {
                mainWindow.Cursor = new Cursor(StandardCursorType.Wait);

                CleanUI();

                // Se hai bisogno del percorso assoluto come stringa (solo su Desktop):
                string absolutePath = selectedFile.Path.LocalPath;

                Console.WriteLine($"File selezionato: {selectedFile.Name}");

                LblNomeFile.Text = $"Elaborazione in corso: {selectedFile.Name}...";
                LblNomeFile.Foreground = Avalonia.Media.Brushes.Orange;

                //***************************************************************
                DatiBolla? datiBolla = await InviaFileAllApiAsync(selectedFile);
                //***************************************************************

                if (datiBolla?.Articoli != null)
                {
                    TxtFornitore.Text = datiBolla.Mittente;
                    CbTipoDocumento.SelectedIndex = 1;
                    TxtNumeroDocumento.Text = datiBolla.NumeroBolla;

                    NumImponibile.Value = (decimal) datiBolla.Imponibile;
                    NumTotale.Value = (decimal) datiBolla.Totale;

                    if (!string.IsNullOrEmpty(datiBolla.DataBolla))
                    {
                        try
                        {
                            // Converte la stringa YYYY/MM/DD in un DateTime classico
                            DateTime dataParsed = DateTime.ParseExact(datiBolla.DataBolla, "yyyy-MM-dd",
                                CultureInfo.InvariantCulture);

                            // Assegna il valore al DatePicker convertendolo in DateTimeOffset
                            DpDataDocumento.SelectedDate = dataParsed;
                        }
                        catch (FormatException)
                        {
                            // Gestisci il caso in care la stringa non sia nel formato corretto
                            DpDataDocumento.SelectedDate = null;
                        }
                    }
                    else
                    {
                        DpDataDocumento.SelectedDate = null;
                    }

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

                ApriPdfEsterno(absolutePath);
            }
            catch (Exception ex)
            {
               await  DialogHelper.ShowErrorDialog(mainWindow, "Errore", ex.Message);
            }
            finally
            {
                mainWindow.Cursor = Cursor.Default; 
            }
                
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private async Task<DatiBolla?> InviaFileAllApiAsync(IStorageFile file)
    {
        string urlApi = $"{App.CurrentApp.EMMMA_ENDPOINT}/api/doc/ddt";

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
                await using var responseStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<DatiBolla>(responseStream);
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnElimina_Click(object? sender, RoutedEventArgs e)
    {
        if (DataGridArticoli.SelectedItem is ArticoloBolla articoloSelezionato)
        {
            ListaArticoli.Remove(articoloSelezionato);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="urlDelFilePdf"></param>
    private void ApriPdfEsterno(string urlDelFilePdf)
    {
        if (File.Exists(urlDelFilePdf))
        {
            try
            {
                var pi = new ProcessStartInfo
                {
                    FileName = urlDelFilePdf,
                    UseShellExecute = true // Fondamentale per dire al sistema di usare l'app predefinita
                };
                Process.Start(pi);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private void Button_SaveOnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}