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
using System.Collections.ObjectModel;

using EmmaClientAv.Forms.Dialog;

namespace EmmaClientAv.Forms.Articoli;

public partial class ArticoliForm : Window
{
    public ObservableCollection<EmmaArticoli> Articoli { get; set; } = new();
    private static readonly HttpClient Client = new HttpClient();
    private bool _autoChiusuraInCorso = false;
    
    public ArticoliForm()
    {
        InitializeComponent();
        
        CbFornitore.SelectionChanged += CbFornitoreOnSelectionChanged;
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
    
    private async  void CaricaDati(string descrizione)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli?fornitore={descrizione}";
        using var request = new HttpRequestMessage(HttpMethod.Get, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var emmaArticoliList = await response.Content.ReadFromJsonAsync<List<EmmaArticoli>>().ConfigureAwait(false);
            if (emmaArticoliList != null)
            {
                emmaArticoliList = emmaArticoliList.OrderBy(x => x.descrizione).ToList();
                
                // Usiamo Avalonia la UI Thread per svuotare e ripopolare in sicurezza
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Articoli.Clear();

                    foreach (var articolo in emmaArticoliList)
                    {
                        // Forza il reset a false dopo la deserializzazione
                        articolo.IsDirty = false; 
            
                        Articoli.Add(articolo);
                    }

                    // Se l'hai già fatto nel costruttore o nello XAML, questa riga puoi anche ometterla
                    ArticoliGrid.ItemsSource = Articoli; 
                });
            }
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
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
    
    private async void ArticoliForm_Closing(object? sender, WindowClosingEventArgs e)
    {

    }

    private void Aggiungi_Click(object? sender, RoutedEventArgs e)
    {
        var fornitoreSelezionato = CbFornitore.SelectedItem as EmmaFornitori;
        if (fornitoreSelezionato != null && !string.IsNullOrWhiteSpace(fornitoreSelezionato.descrizione))
        {

            var nuovoArticolo = new EmmaArticoli()
            {
                codice = "Nuovo Codice ...",
                descrizione = "Nuovo Articolo...",
                idfornitore = fornitoreSelezionato.id
            };

            Articoli.Add(nuovoArticolo);

            // Seleziona automaticamente il nuovo elemento e fai lo scroll
            ArticoliGrid.SelectedItem = nuovoArticolo;
            ArticoliGrid.ScrollIntoView(nuovoArticolo, null);
        }
    }

    private async void Elimina_Click(object? sender, RoutedEventArgs e)
    {
        if (ArticoliGrid.SelectedItem is EmmaArticoli articoloSelezionato)
        {
            // Se l'elemento esiste già sul DB (id > 0), lo eliminiamo dal DB
            if (articoloSelezionato.id > 0)
            {
                try
                {
                    var dialog = new ConfermaDialog();
                    bool? risultato = await dialog.ShowDialog<bool?>(this);
                    if (risultato == false) return;
                        
                    await DeleteArticolo(articoloSelezionato);
                        
                    // Lo rimuoviamo dalla visualizzazione
                    Articoli.Remove(articoloSelezionato);
                }
                catch (Exception ex)
                {

                    await  DialogHelper.ShowErrorDialog(this, "Errore", $"{ex.Message}");
                }
            }
        }
    }
    
    private async Task DeleteArticolo(EmmaArticoli articolo)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli";
        using var request = new HttpRequestMessage(HttpMethod.Delete, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(articolo);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            //
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
        }
    }

    private async void Salva_Click(object? sender, RoutedEventArgs e)
    {
        var fornitoreSelezionato = CbFornitore.SelectedItem as EmmaFornitori;
        if (fornitoreSelezionato == null || string.IsNullOrWhiteSpace(fornitoreSelezionato.descrizione))
        {
            return;
        }
        

        var dialog = new ConfermaDialog();
        bool? risultato = await dialog.ShowDialog<bool?>(this);
        if (risultato == false) return;
            
        // Forza la chiusura dell'editing della cella corrente per scrivere l'ultimo dato nel modello
        ArticoliGrid.CommitEdit();
            
        int aggiornati = 0;
        int inseriti = 0;

        foreach (var articoli in Articoli)
        {
            if (articoli.id == 0)
            {
                await AddArticolo(articoli);
                inseriti++;
            }
            else if (articoli.IsDirty)
            {
                await UpdateArticolo(articoli);
            
                // Resetta il flag dopo il salvataggio avvenuto con successo
                articoli.IsDirty = false; 
                aggiornati++;
            }
        }
            
        // Opzionale: rinfresca la griglia
        if (fornitoreSelezionato != null && !string.IsNullOrWhiteSpace(fornitoreSelezionato.descrizione))
        {
            // Fai qualcosa con il fornitore selezionato
            var descrizione = fornitoreSelezionato.descrizione;
            CaricaDati(descrizione);
        }

        await  DialogHelper.ShowErrorDialog(this, "Informazione", $"Salvataggio completato: {inseriti} inseriti, {aggiornati} aggiornati.");
    }
    
    private async Task AddArticolo(EmmaArticoli articolo)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli";
        using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(articolo);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            //
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
        }
    }
    
    private async Task UpdateArticolo(EmmaArticoli articolo)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/articoli";
        using var request = new HttpRequestMessage(HttpMethod.Put, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        request.Content = JsonContent.Create(articolo);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            //
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
        }
    }

    private void CbFornitoreOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            // Recupera l'elemento selezionato castandolo al tuo tipo di dato
            var fornitoreSelezionato = comboBox.SelectedItem as EmmaFornitori;

            if (fornitoreSelezionato != null && !string.IsNullOrWhiteSpace(fornitoreSelezionato.descrizione))
            {
                // Fai qualcosa con il fornitore selezionato
                var descrizione = fornitoreSelezionato.descrizione;
                CaricaDati(descrizione);
                
                return; 
            }
        }

        Articoli.Clear();
        ArticoliGrid.ItemsSource = Articoli; 
    }
}