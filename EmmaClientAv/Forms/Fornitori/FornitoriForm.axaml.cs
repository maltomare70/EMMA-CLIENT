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


namespace EmmaClientAv.Forms.Fornitori;

public partial class FornitoriForm : Window
{
    public ObservableCollection<EmmaFornitori> Fornitori { get; set; } = new();
    private static readonly HttpClient Client = new HttpClient();
    private bool _autoChiusuraInCorso = false;
    
    public FornitoriForm()
    {
        InitializeComponent();
        
        CaricaDati();

    }
    
    private async  void CaricaDati()
    {
        string urlApi = $"{App.Config.ServerUrl}/api/fornitori";
        using var request = new HttpRequestMessage(HttpMethod.Get, urlApi);
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        HttpResponseMessage response = await Client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var emmaFornitoriList = await response.Content.ReadFromJsonAsync<List<EmmaFornitori>>().ConfigureAwait(false);
            if (emmaFornitoriList != null)
            {
                emmaFornitoriList = emmaFornitoriList.OrderBy(x => x.descrizione).ToList();
                
                // Usiamo Avalonia la UI Thread per svuotare e ripopolare in sicurezza
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Fornitori.Clear();

                    foreach (var fornitore in emmaFornitoriList)
                    {
                        // Forza il reset a false dopo la deserializzazione
                        fornitore.IsDirty = false; 
            
                        Fornitori.Add(fornitore);
                    }

                    // Se l'hai già fatto nel costruttore o nello XAML, questa riga puoi anche ometterla
                    FornitoriGrid.ItemsSource = Fornitori; 
                });
            }
        }
        else
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", $"Errore durante l'invio: {response.StatusCode} {response.Content}");
        }
    }
    
    private async void FornitoriForm_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_autoChiusuraInCorso) return;
        
        // 1. Forza il commit dell'eventuale cella attualmente in fase di editing nella griglia
        FornitoriGrid.CommitEdit();

        // 2. Controlla se ci sono effettivamente modifiche o nuovi inserimenti
        bool haModifiche = Fornitori.Any(f => f.id == 0 || f.IsDirty);

        if (!haModifiche)
        {
            return; // Nessuna modifica, lascia chiudere la finestra normalmente
        }
        
        
        // 3. Blocca temporaneamente la chiusura immediata della finestra per permettere il salvataggio asincrono
        e.Cancel = true;

        var dialog = new ConfermaDialog();
        bool? risultato = await dialog.ShowDialog<bool?>(this);
        if (risultato == false)
        {
            this.Close();
        };
        
        // Rimuoviamo l'evento per evitare un loop infinito quando richiameremo Close() alla fine
        this.Closing -= FornitoriForm_Closing;

        try
        {
            _autoChiusuraInCorso = true; // Evita che il prossimo Close() riesegua questo metodo
            
            int aggiornati = 0;
            int inseriti = 0;

            foreach (var fornitore in Fornitori)
            {
                if (fornitore.id == 0)
                {
                    await AddFornitore(fornitore);
                    inseriti++;
                }
                else if (fornitore.IsDirty)
                {
                    await UpdateFornitore(fornitore);
            
                    // Resetta il flag dopo il salvataggio avvenuto con successo
                    fornitore.IsDirty = false; 
                    aggiornati++;
                }
            }

        }
        catch (Exception ex)
        {
            _autoChiusuraInCorso = false;
            System.Diagnostics.Debug.WriteLine($"Errore durante il salvataggio automatico: {ex.Message}");
        }
        finally
        {
            // 5. Chiudi definitivamente la finestra ora che l'operazione è terminata
            this.Close();
        }
    }
    
    private void Aggiungi_Click(object? sender, RoutedEventArgs e)
        {
            var nuovoFornitore = new EmmaFornitori
            {
                descrizione = "Nuovo Fornitore...",
                riferimento = ""
            };
            
            Fornitori.Add(nuovoFornitore);
            
            // Seleziona automaticamente il nuovo elemento e fai lo scroll
            FornitoriGrid.SelectedItem = nuovoFornitore;
            FornitoriGrid.ScrollIntoView(nuovoFornitore, null);
        }

        // EVENTO CLICK: Elimina la riga selezionata
        private async void Elimina_Click(object? sender, RoutedEventArgs e)
        {
            if (FornitoriGrid.SelectedItem is EmmaFornitori fornitoreSelezionato)
            {
                // Se l'elemento esiste già sul DB (id > 0), lo eliminiamo dal DB
                if (fornitoreSelezionato.id > 0)
                {
                    try
                    {
                        var dialog = new ConfermaDialog();
                        bool? risultato = await dialog.ShowDialog<bool?>(this);
                        if (risultato == false) return;
                        
                        await DeleteFornitore(fornitoreSelezionato);
                        
                        // Lo rimuoviamo dalla visualizzazione
                        Fornitori.Remove(fornitoreSelezionato);
                    }
                    catch (Exception ex)
                    {

                        await  DialogHelper.ShowErrorDialog(this, "Errore", $"{ex.Message}");
                    }
                }
            }
        }

        private async Task DeleteFornitore(EmmaFornitori fornitore)
        {
            string urlApi = $"{App.Config.ServerUrl}/api/fornitori";
            using var request = new HttpRequestMessage(HttpMethod.Delete, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            request.Content = JsonContent.Create(fornitore);
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
        
        // EVENTO CLICK: Salva (Inserisce i nuovi o aggiorna gli esistenti)
        private async void Salva_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new ConfermaDialog();
            bool? risultato = await dialog.ShowDialog<bool?>(this);
            if (risultato == false) return;
            
            // Forza la chiusura dell'editing della cella corrente per scrivere l'ultimo dato nel modello
            FornitoriGrid.CommitEdit();
            
            int aggiornati = 0;
            int inseriti = 0;

            foreach (var fornitore in Fornitori)
            {
                if (fornitore.id == 0)
                {
                    await AddFornitore(fornitore);
                    inseriti++;
                }
                else if (fornitore.IsDirty)
                {
                    await UpdateFornitore(fornitore);
            
                    // Resetta il flag dopo il salvataggio avvenuto con successo
                    fornitore.IsDirty = false; 
                    aggiornati++;
                }
            }
            
            // Opzionale: rinfresca la griglia
            CaricaDati();
            await  DialogHelper.ShowErrorDialog(this, "Informazione", $"Salvataggio completato: {inseriti} inseriti, {aggiornati} aggiornati.");
        }

        private async Task AddFornitore(EmmaFornitori fornitore)
        {
            string urlApi = $"{App.Config.ServerUrl}/api/fornitori";
            using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            request.Content = JsonContent.Create(fornitore);
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
        
        private async Task UpdateFornitore(EmmaFornitori fornitore)
        {
            string urlApi = $"{App.Config.ServerUrl}/api/fornitori";
            using var request = new HttpRequestMessage(HttpMethod.Put, urlApi);
            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{App.CurrentApp.EMMMA_USER}:{App.CurrentApp.EMMMA_PASSWORD}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            request.Content = JsonContent.Create(fornitore);
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
}