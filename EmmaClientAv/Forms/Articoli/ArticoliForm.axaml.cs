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

    }

    private async void Elimina_Click(object? sender, RoutedEventArgs e)
    {
    }

    private async void Salva_Click(object? sender, RoutedEventArgs e)
    {

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