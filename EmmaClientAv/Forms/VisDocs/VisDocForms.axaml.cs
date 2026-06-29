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
using System.Collections.ObjectModel;
using Avalonia.Media;
namespace EmmaClientAv.Forms.VisDocs;

public partial class VisDocForms : Window
{
    public ObservableCollection<MasterDocumento> DocumentiInGriglia { get; set; } = new();
    
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
    
    // private void OnDataGridTemplateClick(object? sender, RoutedEventArgs e)
    // {
    //     if (e.Source is Button btn && btn.Name == "BtnAggiungiArticolo")
    //     {
    //         // Essendo dentro il DataTemplate, il DataContext del pulsante è automaticamente il MasterDocumento corrente
    //         if (btn.DataContext is MasterDocumento master)
    //         {
    //             master.Dettagli?.Add(new RigheDocumento()
    //             {
    //                 CodiceArticolo = "NUOVO",
    //                 DescrizioneArticolo = "Nuovo Articolo",
    //                 UnitaMisura = "PZ",
    //                 Qta = 1,
    //                 Imponibile = 0,
    //                 IVA = "22",
    //                 Totale = 0
    //             });
    //         }
    //     }
    // }
    
    //Pulsante Elimina Riga / Aggiunta Riga
    private async void EliminaRiga_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button bottone && bottone.DataContext is RigheDocumento riga)
        {
            bottone.Background = Brush.Parse("#4CAF50");
            
            // 2. Chiamata all'API per notificare l'eliminazione
            bool apiSuccess = false;

            if (bottone.Content.ToString().ToLower() == "aggiungi")
            {
                _ = await InviaAddAllApi(riga);
            }
            else
            {
                apiSuccess = await InviaEliminazioneAllApi(riga);
                if (apiSuccess && bottone.FindAncestorOfType<DataGrid>()?.DataContext is MasterDocumento master)
                {
                    // Rimuove l'elemento dalla lista (Funziona al meglio se Dettagli è una ObservableCollection)
                    master.Dettagli.Remove(riga);
                }
            }
        }
    }
    
    //Invia API per aggiunta nuova riga
    private async Task<bool> InviaAddAllApi(RigheDocumento riga)
    {
        try
        {

            var articoloBolla = new ArticoloBolla();
            articoloBolla.Id_Master = riga.IdMaster;
            articoloBolla.Id_Riga = Guid.NewGuid().ToString();
            articoloBolla.Quantita = riga.Qta;
            articoloBolla.Descrizione = riga.DescrizioneArticolo;
            articoloBolla.Codice = riga.CodiceArticolo;
            articoloBolla.Imponibile = riga.Imponibile;
            articoloBolla.Totale = riga.Totale;
            articoloBolla.UnitaMisura = riga.UnitaMisura;
            articoloBolla.Iva = riga.IVA;
        
            string urlApi = $"{App.Config.ServerUrl}/api/v1/doc/riga";
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        
            request.Content = JsonContent.Create(articoloBolla);
            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                throw new Exception($"Errore durante l'invio: {response.StatusCode} {response.Content}");
            }
            
        }
        catch
        {
            return false;
        }
    }
    
    //Invia API per eliminazione riga
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
    
    //Modfica valori di riga
    private async void OnElementLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control visualElement && visualElement.DataContext is RigheDocumento rigaModificata)
        {
            // Invia all'API
            await InviaModificaAllApi(rigaModificata);
        }
    }
    
    //Invia Riga Modificata al Server
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

    //per inserire nel combo un elemento nuovo
    private async void Window_OnOpened(object? sender, EventArgs e)
    {
        try
        {
            var fornitori = await GetFornitoriAsync();
            var listaFornitori = fornitori.ToList();
            // Inseriamo un elemento vuoto all'indice 0
            listaFornitori.Insert(0, new EmmaFornitori 
            { 
                descrizione = string.Empty,
            });
                
            CbFornitore.ItemsSource = listaFornitori;
            CbFornitore.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore: {ex.Message}");
        }
    }
    
    //APi per acquisire Fornitori
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
    
    //API per acquisire docuemnti (filtrtati)
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

    //Carico i dati nella Grid
    private void LoadDataGridData(List<EmmaDoc> docs)
    {
        List<MasterDocumento> sampleData = new List<MasterDocumento>();
        foreach (var emmaDoc in docs)
        {
            var doc = emmaDoc?.ToDoc();
            if ( doc is null ) continue;
            
            var master = new MasterDocumento()
            {
                Id = doc.Id,
                Fornitore = doc.Mittente, 
                NumeroDocumento = doc.NumeroBolla, 
                DataDocumento = doc.DataBolla,
                StatoDocumento = emmaDoc?.stato == 0 ? "Aperto" : "Chiuso",
                TipDocumento = doc.TipoDocumento
                
            };

            ObservableCollection<RigheDocumento> dettagli = new ObservableCollection<RigheDocumento>();
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

        var datiOrdinati = sampleData.OrderBy((x=>x.Fornitore))
            .ThenBy(u => u.DataDocumento)
            .ThenBy(u => u.NumeroDocumento)
            .ToList();
        
        DocumentiInGriglia = new ObservableCollection<MasterDocumento>(datiOrdinati);

        // 3. Assegniamo la ObservableCollection alla DataGrid
        DataGridArticoli.ItemsSource = DocumentiInGriglia;
    }
    
    //Evento per aggiungere una nuova Riga
    private void AggiungiRiga_Click()
    {
        // 1. Recupera il Master attualmente selezionato nella DataGrid
        if (DataGridArticoli.SelectedItem is MasterDocumento masterSelezionato)
        {
            // 2. Crea la nuova riga di dettaglio
            var nuovaRiga = new RigheDocumento
            {
                IdRiga = "",
                IdMaster = masterSelezionato.Id, // Se hai un ID di collegamento
                CodiceArticolo = "",
                DescrizioneArticolo = "",
                UnitaMisura = "",
                Qta = 0,
                Imponibile = 0,
                IVA =  "",
                Totale = 0
            };
            
            masterSelezionato.Dettagli.Add(nuovaRiga);
        }
        else
        {
            // Opzionale: avvisa l'utente che deve prima selezionare una riga Master
        }
    }
    
    //Mostra documenti
    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        await CaricaDati();
    }
    
    //Aprire / Chiudere documento
    private async void Button_OnClick_2(object? sender, RoutedEventArgs e)
    {
        await CaricaDati();
    }

    //Ricaricaricare i Dati
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

    //Per gestire l'apertura e Chiusura delle sezioni
    private void DataGridArticoli_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // per gestire la cattura del click sul pulsante aggiungi tiga
        // che l'evento click non riesce a catturare
        if (e.Source is Visual visualSource)
        {
            var buttonAncestor = visualSource.FindAncestorOfType<Button>(includeSelf: true);
        
            if (buttonAncestor != null && buttonAncestor.Tag?.ToString() == "additem")
            {
                AggiungiRiga_Click();
                
                return; 
            }
        }
        

        var row = (e.Source as Visual)?.FindAncestorOfType<DataGridRow>();
        if (row != null)
        {
            if (DataGridArticoli.SelectedItem == row.DataContext)
            {
                DataGridArticoli.SelectedItem = null;
                e.Handled = true;
            }
        }
    }
}