using Avalonia.Controls;
using EmmaServer.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using EmmaClientAv.Helpers;
using System.Collections.ObjectModel;

using EmmaClientAv.Forms.Dialog;
using EmmaClientAv.Services;

namespace EmmaClientAv.Forms.Articoli;

public partial class ArticoliForm : Window
{
    public ObservableCollection<EmmaArticoli> Articoli { get; set; } = new();
    private bool _autoChiusuraInCorso = false;
    private readonly IArticoliService _articoliService;
    private readonly IFornitoriService _fornitoreService;
    public ArticoliForm()
    {
        InitializeComponent();
        _articoliService = new ArticoliService(App.Config.ServerUrl, App.CurrentApp.EMMMA_USER, App.CurrentApp.EMMMA_PASSWORD);
        _fornitoreService = new FornitoriService(App.Config.ServerUrl, App.CurrentApp.EMMMA_USER, App.CurrentApp.EMMMA_PASSWORD);
        
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
            var fornitori = await _fornitoreService.GetFornitoriAsync();
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
    
    private async  Task CaricaDati(string descrizione)
    {
        var emmaArticoliList = await _articoliService.GetArticoliFornitore(descrizione);
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
    
    
    private async void ArticoliForm_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_autoChiusuraInCorso) return;

        // 1. Forza il commit dell'eventuale cella attualmente in fase di editing nella griglia
        ArticoliGrid.CommitEdit();

        // 2. Controlla se ci sono effettivamente modifiche o nuovi inserimenti
        bool haModifiche = Articoli.Any(f => f.id == 0 || f.IsDirty);

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
        }
        ;

        // Rimuoviamo l'evento per evitare un loop infinito quando richiameremo Close() alla fine
        this.Closing -= ArticoliForm_Closing;

        try
        {
            _autoChiusuraInCorso = true; // Evita che il prossimo Close() riesegua questo metodo

            int aggiornati = 0;
            int inseriti = 0;

            foreach (var articolo in Articoli)
            {
                if (articolo.id == 0)
                {
                    await _articoliService.AddArticolo(articolo);
                    inseriti++;
                }
                else if (articolo.IsDirty)
                {
                    await _articoliService.UpdateArticolo(articolo);

                    // Resetta il flag dopo il salvataggio avvenuto con successo
                    articolo.IsDirty = false;
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
                        
                    await _articoliService.DeleteArticolo(articoloSelezionato);
                        
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
                await _articoliService.AddArticolo(articoli);
                inseriti++;
            }
            else if (articoli.IsDirty)
            {
                await _articoliService.UpdateArticolo(articoli);
            
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
            await CaricaDati(descrizione);
        }

        await  DialogHelper.ShowErrorDialog(this, "Informazione", $"Salvataggio completato: {inseriti} inseriti, {aggiornati} aggiornati.");
    }
    

    private async void CbFornitoreOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            // Recupera l'elemento selezionato castandolo al tuo tipo di dato
            var fornitoreSelezionato = comboBox.SelectedItem as EmmaFornitori;

            if (fornitoreSelezionato != null && !string.IsNullOrWhiteSpace(fornitoreSelezionato.descrizione))
            {
                // Fai qualcosa con il fornitore selezionato
                var descrizione = fornitoreSelezionato.descrizione;
                await CaricaDati(descrizione);
                
                return; 
            }
        }

        Articoli.Clear();
        ArticoliGrid.ItemsSource = Articoli; 
    }
}