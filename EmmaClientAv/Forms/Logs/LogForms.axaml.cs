using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Emma.Services.Services;
using EmmaClientAv.Services;
using EmmaServer.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmmaClientAv;

public partial class LogForms : Window
{
    private LogService _logService;
    public List<EmmaLog> Logs { get; set; } = new();
    public LogForms()
    {
        InitializeComponent();

        _logService = new LogService(App.Config.ServerUrl, App.CurrentApp.EMMMA_USER, App.CurrentApp.EMMMA_PASSWORD);


        CaricaDati();
    }

    private async void CaricaDati()
    {
        var logList = await _logService.GetAllAsync();
        if (logList != null)
        {

            // Usiamo Avalonia la UI Thread per svuotare e ripopolare in sicurezza
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Logs.Clear();

                foreach (var log in logList)
                {
                    Logs.Add(log);
                }

                // Se l'hai già fatto nel costruttore o nello XAML, questa riga puoi anche ometterla
                LogGrid.ItemsSource = Logs.OrderByDescending(x=>x.data_creazione);
            });
        }
    }
}