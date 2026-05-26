using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace EmmaClientAv.Helpers;

public static class DialogHelper
{
    public static Task ShowErrorDialog(Window parentWindow, string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right
        };

        // Gestiamo la chiusura qui senza scomodare ReactiveUI
        okButton.Click += (s, e) => dialog.Close();

        var panel = new StackPanel
        {
            Spacing = 15,
            Margin = new Avalonia.Thickness(20)
        };

        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        panel.Children.Add(okButton);

        dialog.Content = panel;

        return dialog.ShowDialog(parentWindow);
    }
}