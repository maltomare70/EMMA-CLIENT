using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;
using EmmaServer.Entities;
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Avalonia.Interactivity;
using EmmaClientAv.Helpers;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using Avalonia.Media;
namespace EmmaClientAv.Forms.VisDocs;

public partial class VisDocForms : Window
{
    private ObservableCollection<MasterDocumento> DocumentiInGriglia { get; set; } = new();
    private static readonly HttpClient Client = new HttpClient();
    
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

        DataGridArticoli.AddHandler(InputElement.LostFocusEvent, 
            OnElementLostFocus, RoutingStrategies.Bubble);
    }


    private async Task CambioStato(MasterDocumento masterDocumento)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/v1/doc/stato";
        using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(new CambioStato()
        {
            Id = masterDocumento.Id,
            Stato = masterDocumento.StatoDocumento == "Aperto" ? 1 : 0
        });
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return;
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
            return;
        }
    }

    int GetTipoDocumento(string tipodoc)
    {
        return int.Parse(tipodoc);
    }
    
    private async Task CancellaDocumento(MasterDocumento masterDocumento)
    {
        EmmaDocFilters emmaDocFilters = new()
        {
            Fornitore =  masterDocumento.Fornitore,
            NumeroDoc = masterDocumento.NumeroDocumento,
            DataDoc = masterDocumento.DataDocumento,
            TipoDoc = GetTipoDocumento(masterDocumento.TipDocumento),
            Stato = masterDocumento.StatoDocumento == "Aperto" ? 0 : 1
        };
        
        string urlApi = $"{App.Config.ServerUrl}/api/v1/doc";
        using var request = new HttpRequestMessage(HttpMethod.Delete, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(emmaDocFilters);
        
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            await CaricaDati();
            
            return;
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
            return;
        }
    }
    
    private async void Button_OnElimina(object sender, RoutedEventArgs e)
    {
        // Recupera l'oggetto legato alla riga corrente
        var button = sender as Button;
        var documento = button?.DataContext as MasterDocumento;

        if (documento != null)
        {
            await CancellaDocumento(documento);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void EliminaRiga_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button bottone && bottone.DataContext is RigheDocumento riga)
        {
            bottone.Background = Brush.Parse("#4CAF50");
            
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
                    // Rimuove l'elemento dalla lista
                    master.Dettagli.Remove(riga);
                }
            }
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="riga"></param>
    /// <returns></returns>
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
            using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        
            request.Content = JsonContent.Create(articoloBolla);
            HttpResponseMessage response = await Client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="riga"></param>
    /// <returns></returns>
    private async Task<bool> InviaEliminazioneAllApi(RigheDocumento riga)
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
            using var request = new HttpRequestMessage(HttpMethod.Delete, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        
            request.Content = JsonContent.Create(articoloBolla);
            HttpResponseMessage response = await Client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnElementLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control visualElement && visualElement.DataContext is RigheDocumento rigaModificata)
        {
            // Invia all'API
            await InviaModificaAllApi(rigaModificata);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="riga"></param>
    private async Task InviaModificaAllApi(RigheDocumento riga)
    {
        try
        {
            if (riga is null) return;
            
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
            using var request = new HttpRequestMessage(HttpMethod.Put, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            
            request.Content = JsonContent.Create(articoloBolla);
            HttpResponseMessage response = await Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
            }
        }
        catch (Exception ex)
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"{ex.Message}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"{ex.Message}");
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    async Task<List<EmmaFornitori>> GetFornitoriAsync()
    {
        string urlApi = $"{App.Config.ServerUrl}/api/fornitori";
        using var request = new HttpRequestMessage(HttpMethod.Get, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var emmaFornitoriList = await response.Content.ReadFromJsonAsync<List<EmmaFornitori>>().ConfigureAwait(false);
            return emmaFornitoriList?.ToList() ?? new List<EmmaFornitori>();
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
            return new List<EmmaFornitori>();
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="docFilters"></param>
    /// <returns></returns>
    async Task<List<EmmaDoc>> GetDocsAsync(EmmaDocFilters docFilters)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/v1/doc";
        using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(docFilters);
    
        try
        {
            // Use the shared client instance
            HttpResponseMessage response = await Client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var emmaDocList = await response.Content.ReadFromJsonAsync<List<EmmaDoc>>();
                return emmaDocList ?? new List<EmmaDoc>();
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
            
                await DialogHelper.ShowErrorDialog(
                    this, 
                    "Errore", 
                    $"Errore durante l'invio: {(int)response.StatusCode} {response.ReasonPhrase}\nDettagli: {errorContent}"
                );
            
                return new List<EmmaDoc>();
            }
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorDialog(this, "Errore di rete", ex.Message);
            return new List<EmmaDoc>();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="docs"></param>
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
        DataGridArticoli.ItemsSource = DocumentiInGriglia;
    }
    
    /// <summary>
    /// 
    /// </summary>
    private async void AggiungiRiga_Click()
    {
        if (DataGridArticoli.SelectedItem is MasterDocumento masterSelezionato)
        {
            // 2. Crea la nuova riga di dettaglio
            var nuovaRiga = new RigheDocumento
            {
                IdRiga = "",
                IdMaster = masterSelezionato.Id,
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
            await DialogHelper.ShowErrorDialog(this, "Informazione", "selezionare una riga.");
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await CaricaDati();
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorDialog(this, "Errore", ex.Message);
        }
    }
    
   /// <summary>
   /// 
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
    private async void Button_OnCambioStato(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control visualElement && visualElement.DataContext is MasterDocumento master)
        {
            await CambioStato(master);
            await CaricaDati();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private async Task CaricaDati()
    {
        if ( CbTipoDocumento.SelectedIndex < 0)
        {
            await  DialogHelper.ShowErrorDialog(this, "Informazione", $"Scegli un Tipo Documento");
            return;
        }
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

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DataGridArticoli_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // per gestire la cattura del click sul pulsante aggiungi riga
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