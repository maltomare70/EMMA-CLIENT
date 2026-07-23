using Avalonia.Controls;
using EmmaServer.Entities;
using System;
using System.Linq;
using Avalonia.Interactivity;
using EmmaClientAv.Helpers;
using System.Collections.ObjectModel;

using EmmaClientAv.Forms.Dialog;
using EmmaClientAv.Services;

namespace EmmaClientAv.Forms.Fornitori;

public partial class FornitoriForm : Window
{
    public ObservableCollection<EmmaFornitori> Fornitori { get; set; } = new();
    private bool _autoChiusuraInCorso = false;
    private readonly IFornitoriService _fornitoriService;
    
    public FornitoriForm()
    {
        InitializeComponent();
        
        _fornitoriService = new FornitoriService(App.Config.ServerUrl, App.CurrentApp.EMMMA_USER, App.CurrentApp.EMMMA_PASSWORD);
        
        CaricaDati();
    }
    
    private async  void CaricaDati()
    {
        var emmaFornitoriList = await _fornitoriService.GetFornitoriAsync();
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

    private async void FornitoriForm_Closing(object? sender, WindowClosingEventArgs e)
    {
        // 1. Se stiamo già chiudendo programmaticamente, lascia passare la chiusura
        if (_autoChiusuraInCorso) return;

        // 2. Forza il commit dell'eventuale cella attualmente in fase di editing nella griglia
        FornitoriGrid.CommitEdit();

        // 3. Controlla se ci sono effettivamente modifiche o nuovi inserimenti
        bool haModifiche = Fornitori.Any(f => f.id == 0 || f.IsDirty);

        if (!haModifiche)
        {
            return; // Nessuna modifica: lascia chiudere la finestra normalmente
        }

        // 4. Annulla SUBITO la chiusura per gestire il flusso asincrono
        e.Cancel = true;

        // 5. Chiedi conferma all'utente
        var dialog = new ConfermaDialog();
        bool? risultato = await dialog.ShowDialog<bool?>(this);

        // Se l'utente chiude il dialog con la X (null), annulliamo l'azione e lasciamo la finestra aperta
        if (risultato == null)
        {
            return;
        }

        // Unsubscribe dall'evento prima di qualsiasi Close() manuale per evitare loop
        this.Closing -= FornitoriForm_Closing;

        // Se l'utente sceglie NO (false): chiudi direttamente senza salvare
        if (risultato == false)
        {
            this.Close();
            return;
        }

        // Se l'utente sceglie SÌ (true): procedi con il salvataggio
        try
        {
            _autoChiusuraInCorso = true;

            int aggiornati = 0;
            int inseriti = 0;

            foreach (var fornitore in Fornitori)
            {
                if (fornitore.id == 0)
                {
                    await _fornitoriService.AddFornitore(fornitore);
                    inseriti++;
                }
                else if (fornitore.IsDirty)
                {
                    await _fornitoriService.UpdateFornitore(fornitore);

                    // Resetta il flag dopo il salvataggio avvenuto con successo
                    fornitore.IsDirty = false;
                    aggiornati++;
                }
            }

            // Salvataggio completato con successo: chiudi la finestra
            this.Close();
        }
        catch (Exception ex)
        {
            // In caso di errore ripristiniamo lo stato e l'evento così l'utente può riprovare
            _autoChiusuraInCorso = false;
            this.Closing += FornitoriForm_Closing;

            System.Diagnostics.Debug.WriteLine($"Errore durante il salvataggio automatico: {ex.Message}");
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
                        
                        await _fornitoriService.DeleteFornitore(fornitoreSelezionato);
                        
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
                    await _fornitoriService.AddFornitore(fornitore);
                    inseriti++;
                }
                else if (fornitore.IsDirty)
                {
                    await _fornitoriService.UpdateFornitore(fornitore);
            
                    // Resetta il flag dopo il salvataggio avvenuto con successo
                    fornitore.IsDirty = false; 
                    aggiornati++;
                }
            }
            
            // Opzionale: rinfresca la griglia
            CaricaDati();
            await  DialogHelper.ShowErrorDialog(this, "Informazione", $"Salvataggio completato: {inseriti} inseriti, {aggiornati} aggiornati.");
        }
}