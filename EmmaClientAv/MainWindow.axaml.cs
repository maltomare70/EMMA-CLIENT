using Avalonia.Controls;
using Avalonia.Interactivity;
using EmmaClientAv.Forms.LoadDoc;
using EmmaClientAv.Forms.VisDocs;
using  EmmaClientAv.Forms.Fornitori;

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
                
                case "BtnVisualizza":
                    var visDocForms = new VisDocForms();
                    visDocForms.WindowState = WindowState.Maximized;
                    visDocForms.ShowDialog(this);
                    break;

                case "BtnFornitori":
                    var fornioriForms = new FornitoriForm();
                    fornioriForms.WindowState = WindowState.Maximized;
                    fornioriForms.ShowDialog(this);
                    break;

                case "BtnConfigurazione":
                    // Apri form Impostazioni
                    break;
            }
        }
    }
}