using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System.Collections.Generic;
using EmmaServer.Entities; // Adjust based on your actual architecture
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Avalonia.Interactivity;
using EmmaClientAv.Helpers;
using EmmaServer.Entities;
using System.Text.Json;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace EmmaClientAv.Forms.VisDocs;

public partial class VisDocForms : Window
{
    public VisDocForms()
    {
        InitializeComponent();
        
        CbStatoDocumento.Items.Add("0. Aperto");
        CbStatoDocumento.Items.Add("1. Chiuso");

        CbStatoDocumento.SelectedIndex = 0;
        
        CbTipoDocumento.Items.Add("1. Ordine");
        CbTipoDocumento.Items.Add("2. DDT");
        CbTipoDocumento.Items.Add("3. Fattura Accompagnatoria");
        CbTipoDocumento.Items.Add("4. Fattura");
        CbTipoDocumento.Items.Add("5. Nota di Accredito");
        
        DataGridArticoli.AddHandler(
            InputElement.PointerPressedEvent, 
            DataGridArticoli_OnPointerPressed, 
            RoutingStrategies.Tunnel, 
            handledEventsToo: true
        );
    }
    
    
    private async void Window_OnOpened(object? sender, EventArgs e)
    {
        try
        {
            var fornitori = await GetFornitoriAsync();
            
            // Convertiamo in lista (se non lo è già) per poter usare .Insert()
            var listaFornitori = fornitori.ToList();

            // Inseriamo un elemento vuoto all'indice 0
            listaFornitori.Insert(0, new EmmaFornitori 
            { 
                descrizione = string.Empty, // Oppure string.Empty o "-"
                // Se la classe ha un ID (es. IdFornitore), puoi metterlo a 0 o null per riconoscerlo
            });
                
            CbFornitore.ItemsSource = listaFornitori;
            CbFornitore.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore: {ex.Message}");
        }
    }
    async Task<List<EmmaFornitori>> GetFornitoriAsync()
    {
        string urlApi = $"{App.Config.ServerUrl}/api/fornitori";
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        HttpResponseMessage response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var emmaFornitoriList = await response.Content.ReadFromJsonAsync<List<EmmaFornitori>>().ConfigureAwait(false);
            return emmaFornitoriList.ToList();
        }
        else
        {
            throw new Exception($"Errore durante l'invio: {response.StatusCode} {response.Content}");
        }
    }
    
    async Task<List<EmmaDoc>> GetDocsAsync(EmmaDocFilters docFilters )
    {
        string urlApi = $"{App.Config.ServerUrl}/api/v1/doc";
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(docFilters);
        HttpResponseMessage response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var emmaDocList = await response.Content.ReadFromJsonAsync<List<EmmaDoc>>().ConfigureAwait(false);
            return emmaDocList.ToList();
        }
        else
        {
            throw new Exception($"Errore durante l'invio: {response.StatusCode} {response.Content}");
        }
    }
    
    private void LoadDataGridData(List<EmmaDoc> docs)
    {
        var sampleData = new List<MasterDocumento>();
        foreach (var emmaDoc in docs)
        {
            var r =emmaDoc?.content?.Deserialize<DdtResponse>();
            var doc = r?.Document;
            
            var master = new MasterDocumento()
            {
                Fornitore = doc.Mittente, 
                NumeroDocumento = doc.NumeroBolla, 
                DataDocumento = doc.DataBolla,
                StatoDocumento = "Aperto",
                TipDocumento = doc.TipoDocumento
                
            };

            List<RigheDocumento> dettagli = new List<RigheDocumento>();
            foreach (var articolo in doc.Articoli)
            {
                dettagli.Add((new RigheDocumento()
                {
                    CodiceArticolo  = articolo.Codice,
                    DescrizioneArticolo = articolo.Descrizione,
                    Qta = articolo.Quantita,
                    IVA = articolo.Iva,
                    Imponibile = articolo.Imponibile,
                    Totale = articolo.Totale,
                    UnitaMisura = articolo.UnitaMisura,
                }));
            }
            
            master.Dettagli = dettagli;
            sampleData.Add(master); 
        }

        DataGridArticoli.ItemsSource = sampleData;
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if ( CbTipoDocumento.SelectedIndex < 0) return;
        var tipoDoc = CbTipoDocumento.SelectedIndex + 1;

        if ( CbStatoDocumento.SelectedIndex < 0) return;
        var statoDoc = CbStatoDocumento.SelectedIndex;
        
        var item = CbFornitore.SelectedItem;
        EmmaFornitori fornitore = item as EmmaFornitori;
            
        EmmaDocFilters docFilters = new EmmaDocFilters()
        {
            Stato = statoDoc,
            TipoDoc = tipoDoc,
            Fornitore = fornitore?.descrizione ?? string.Empty
        };
        var docs = await GetDocsAsync(docFilters);
        LoadDataGridData(docs);
    }

    private void DataGridArticoli_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Cerchiamo se l'elemento cliccato (Source) è dentro una DataGridRow
        var row = (e.Source as Visual)?.FindAncestorOfType<DataGridRow>();

        if (row != null)
        {
            // Se la riga cliccata è già quella attualmente selezionata
            if (DataGridArticoli.SelectedItem == row.DataContext)
            {
                // Deselezioniamo (questo chiude il dettaglio)
                DataGridArticoli.SelectedItem = null;
                
                // Comunichiamo ad Avalonia che abbiamo gestito il click 
                // evitando che la griglia lo ri-selezioni subito dopo
                e.Handled = true;
            }
        }
    }
}