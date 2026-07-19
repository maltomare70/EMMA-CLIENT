using Avalonia.Controls;
using Avalonia.Interactivity;
using Emma.Services.Services;
using EmmaClientAv.Helpers;


namespace EmmaClientAv;

public partial class PwdForm : Window
{
    public PwdForm()
    {
        InitializeComponent();
        
    }

    private async void Esegui_Click(object sender, RoutedEventArgs e)
    {
        string? nuovaPassword = NewPasswordBox.Text;
        string? confermaPassword = ConfirmPasswordBox.Text;

        if (string.IsNullOrWhiteSpace(nuovaPassword))
        {
            await DialogHelper.ShowErrorDialog(this, "Errore", "Inserire Password !");
            return;
        }

        if (nuovaPassword != confermaPassword)
        {
            await DialogHelper.ShowErrorDialog(this, "Errore", "Password non coincidenti !");
            return;
        }

        if (!PasswordValidator.IsPasswordValid(nuovaPassword))
        {
            await DialogHelper.ShowErrorDialog(this, "Errore", "La password deve essere lunga almeno 8 caratteri, contenere almeno una lettera maisucola un numero e un carattere speciale.");
            return;
        }

        IUserService userService = new UserService(App.Config.ServerUrl, App.CurrentApp.EMMMA_USER, App.CurrentApp.EMMMA_PASSWORD);
        var userResponse = await userService.CambiaPasswordAsync(new EmmaServer.Entities.CambiaPasswordRequest()
        {
            email = App.CurrentApp.EMMMA_USER,
            oldPassword = App.CurrentApp.EMMMA_PASSWORD,
            newPassword = nuovaPassword
        });

        if (userResponse == 1)
        {
            await DialogHelper.ShowErrorDialog(this, "Informazione", "Password cambiata con successo !");

            Close(); 
        }
    }

    private void Annulla_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}