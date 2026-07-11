using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;
using EmmaServer.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using EmmaClientAv.Helpers;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using Avalonia.Media;
using EmmaClientAv.Forms.Dialog;
using EmmaClientAv.Services;
using System.Globalization;
using Avalonia.Data.Converters;

namespace EmmaClientAv.Forms.VisDocs;

public class TestoToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? testo = value as string;

        if (testo == "Elimina")
        {
            // Ritorna il rosso che usavi prima
            return Brush.Parse("#FF4D4D");
        }

        // Colore di default se il testo è diverso (es. Grigio o Trasparente)
        return Brush.Parse("#4CAF50"); //"#7F7F7F"
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}
public partial class VisDocForms : Window
{
    
    private ObservableCollection<MasterDocumento> DocumentiInGriglia { get; set; } = new();
    private readonly IFornitoriService _fornitoriService;
    private readonly IDocService _docService;
    public VisDocForms()
    {
        InitializeComponent();

        _fornitoriService = new FornitoriService(App.Config.ServerUrl, App.CurrentApp.EMMMA_USER, App.CurrentApp.EMMMA_PASSWORD);
        _docService = new DocService(App.Config.ServerUrl, App.CurrentApp.EMMMA_USER, App.CurrentApp.EMMMA_PASSWORD);

        var stati = ArticoliServiceManager.GetStatodocs();
        for (int i = 0; i < stati.Length; i++)
        {
            CbStatoDocumento.Items.Add(stati[i]);
        }
        CbStatoDocumento.SelectedIndex = 0;
        
        var items = ArticoliServiceManager.GetTipodocs();
        for (int i = 0; i < items.Length; i++)
        {
            CbTipoDocumento.Items.Add(items[i]);    
        }
        
        DataGridArticoli.AddHandler(
            InputElement.PointerPressedEvent, 
            DataGridArticoli_OnPointerPressed, 
            RoutingStrategies.Tunnel, 
            handledEventsToo: true
        );

        DataGridArticoli.AddHandler(InputElement.LostFocusEvent, 
            OnElementLostFocus, RoutingStrategies.Bubble);
    }
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Button_OnElimina(object sender, RoutedEventArgs e)
    {
        var dialog = new ConfermaDialog();
        bool? risultato = await dialog.ShowDialog<bool?>(this);
        if (risultato == false) return;
      
        
        // Recupera l'oggetto legato alla riga corrente
        var button = sender as Button;
        var documento = button?.DataContext as MasterDocumento;

        if (documento != null)
        {
            try
            {
                this.Cursor = new Cursor(StandardCursorType.Wait);
                await _docService.CancellaDocumento(documento);
                await CaricaDati();
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorDialog(this, "Errore", ex.Message);
            }
            finally
            {
                this.Cursor = Cursor.Default;
            }
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
            //bottone.Background = Brush.Parse("#4CAF50");
            
            if (bottone.Content.ToString().ToLower() == "aggiungi")
            {
                var dialog = new ConfermaDialog();
                bool? risultato = await dialog.ShowDialog<bool?>(this);
                if (risultato == false) return;

                try
                {
                    _ = await _docService.InviaAddAllApi(riga);

                    bottone.Content = "Elimina";
                    bottone.Background =  Brush.Parse("#FF4D4D");
                }
                catch (Exception exception)
                {
                    await  DialogHelper.ShowErrorDialog(this, "Errore", $"{exception.Message}");
                }
            }
            else
            {
                var dialog = new ConfermaDialog();
                bool? risultato = await dialog.ShowDialog<bool?>(this);
                if (risultato == false) return;
                try
                {
                    bool apiSuccess = await _docService.InviaEliminazioneAllApi(riga);
                    if (apiSuccess && bottone.FindAncestorOfType<DataGrid>()?.DataContext is MasterDocumento master)
                    {
                        // Rimuove l'elemento dalla lista
                        master.Dettagli.Remove(riga);
                    }
                }
                catch (Exception exception)
                {
                    await DialogHelper.ShowErrorDialog(this, "Errore", exception.Message);
                }
                
            }
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnElementLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control visualElement && visualElement.DataContext is RigheDocumento riga)
        {
            try
            {
                if (e.Source is Button)
                {
                    var b = (Button)e.Source;
                    if (b is not null && b.Content == "Elimina") return;
                }

                this.Cursor = new Cursor(StandardCursorType.Wait);
                if (riga is null || string.IsNullOrWhiteSpace(riga.IdRiga)) return;
            
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

                await _docService.InviaModificaAllApi(articoloBolla);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorDialog(this, "Errore", ex.Message);
            }
            finally
            {
                this.Cursor = Cursor.Default;
            }
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
            var fornitori = await _fornitoriService.GetFornitoriAsync();
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
    private async void  AggiungiRiga_Click()
    {
        if (DataGridArticoli.SelectedItem is MasterDocumento masterSelezionato)
        {
            // 2. Crea la nuova riga di dettaglio
            var nuovaRiga = new RigheDocumento
            {
                IdRiga = "",
                IdMaster = masterSelezionato.Id,
                CodiceArticolo = "Nuovo Codice",
                DescrizioneArticolo = "Nuova Descrizione",
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
            this.Cursor = new Cursor(StandardCursorType.Wait);
            await CaricaDati();
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowErrorDialog(this, "Errore", ex.Message);
        }
        finally
        {
            this.Cursor = Cursor.Default;
        }
    }
    
   /// <summary>
   /// 
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
    private async void Button_OnCambioStato(object? sender, RoutedEventArgs e)
    {
        var dialog = new ConfermaDialog();
        bool? risultato = await dialog.ShowDialog<bool?>(this);
        if (risultato == false) return;
        
        if (e.Source is Control visualElement && visualElement.DataContext is MasterDocumento master)
        {
            try
            {
                this.Cursor = new Cursor(StandardCursorType.Wait);
                await _docService.CambioStato(master);
                await CaricaDati();
            }
            catch (Exception exception)
            {
                await DialogHelper.ShowErrorDialog(this, "Errore", exception.Message);
            }
            finally
            {
                this.Cursor = Cursor.Default;
            }
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
        var tipoDoc = CbTipoDocumento.SelectedIndex;

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
        var docs = await _docService.GetDocsAsync(docFilters);
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