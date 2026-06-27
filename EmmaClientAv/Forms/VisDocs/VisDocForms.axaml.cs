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

        DataGridArticoli.AddHandler(InputElement.LostFocusEvent, OnElementLostFocus, RoutingStrategies.Bubble);
    }

    
    private async void EliminaRiga_Click(object? sender, RoutedEventArgs e)
    {
        // 1. Identifichiamo il bottone che è stato cliccato
        if (sender is Button bottone && bottone.DataContext is RigheDocumento rigaDaEliminare)
        {
            // 2. Chiamata all'API per notificare l'eliminazione
            bool apiSuccess = await InviaEliminazioneAllApi(rigaDaEliminare);

            if (apiSuccess)
            {
                // 3. Troviamo la lista dei dettagli per rimuovere la riga dalla UI.
                // Per farlo, cerchiamo il MasterDocumento risalendo l'albero visivo o tramite il parent.
                if (bottone.FindAncestorOfType<DataGrid>()?.DataContext is MasterDocumento master)
                {
                    // Rimuove l'elemento dalla lista (Funziona al meglio se Dettagli è una ObservableCollection)
                    //master.Dettagli.Remove(rigaDaEliminare);

                    
                    await CaricaDati();
  
                }
            }
        }
    }
    
    private async Task<bool> InviaEliminazioneAllApi(RigheDocumento riga)
    {
        try
        {
            //TODO delete riga
            return true; // Ritorna true se andato a buon fine
        }
        catch
        {
            return false;
        }
    }
    
    private async void OnElementLostFocus(object? sender, RoutedEventArgs e)
    {
        // Verifichiamo se l'elemento che ha perso il focus fa parte di una riga di dettaglio
        if (e.Source is Control visualElement && visualElement.DataContext is RigheDocumento rigaModificata)
        {
            // Invia all'API
            await InviaModificaAllApi(rigaModificata);
        }
    }
    
    private async Task InviaModificaAllApi(RigheDocumento riga)
    {
        try
        {
            var articoloBolla = new ArticoloBolla();
            articoloBolla.Id_Master = riga.IdMaster;
            articoloBolla.Id_Riga = riga.IdRiga;
            articoloBolla.Quantita = riga.Qta;
            articoloBolla.Descrizione = riga.DescrizioneArticolo;
            articoloBolla.Codice = riga.CodiceArticolo;
            articoloBolla.Imponibile = riga.Imponibile;
            articoloBolla.Totale = riga.Totale;
            articoloBolla.UnitaMisura = riga.UnitaMisura;
            articoloBolla.Iva = riga.IVA;
            
            string urlApi = $"{App.Config.ServerUrl}/api/v1/doc/riga";
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Put, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            
            request.Content = JsonContent.Create(articoloBolla);
            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                //
            }
            else
            {
                throw new Exception($"Errore durante l'invio: {response.StatusCode} {response.Content}");
            }
        }
        catch (Exception ex)
        {
            // Gestisci l'errore di rete (es. API offline)
            System.Diagnostics.Debug.WriteLine($"Errore di rete: {ex.Message}");
        }
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
                    IdRiga = articolo.Id_Riga,
                    IdMaster = articolo.Id_Master,
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

        DataGridArticoli.ItemsSource = sampleData.OrderBy((x=>x.Fornitore))
            .ThenBy(u => u.DataDocumento)
            .ThenBy(u => u.NumeroDocumento)
            .ToList();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        await CaricaDati();
    }
    
    private async void Button_OnClick_2(object? sender, RoutedEventArgs e)
    {
        await CaricaDati();
    }

    private async Task CaricaDati()
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