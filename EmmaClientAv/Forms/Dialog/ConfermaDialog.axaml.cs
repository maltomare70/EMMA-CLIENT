using Avalonia.Controls;
using Avalonia.Interactivity;

namespace EmmaClientAv.Forms.Dialog;

public partial class ConfermaDialog : Window
{
    public ConfermaDialog()
    {
        InitializeComponent();
    }
    
    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        Close(true); // Restituisce true se clicca Sì
    }

    private void OnNoClick(object sender, RoutedEventArgs e)
    {
        Close(false); // Restituisce false se clicca No
    }
}