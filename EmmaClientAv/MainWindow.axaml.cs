using Avalonia.Controls;
using Avalonia.Interactivity;
using EmmaClientAv.Forms.LoadDoc;

namespace EmmaClientAv;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button bottonePremuto)
        {
            switch (bottonePremuto.Name)
            {

                case "BtnFattura":
                    var loadFormsDDT = new LoadDdtForm();
                    loadFormsDDT.WindowState = WindowState.Maximized;
                    loadFormsDDT.Show();
                    break;
                

                case "BtnConciliazione":
                    // Apri form Conciliazione
                    break;

                case "BtnSincronizzazione":
                    // Avvia logica o apri form Sincronizzazione
                    break;

                case "BtnConfigurazione":
                    // Apri form Impostazioni
                    break;
            }
        }
    }
}