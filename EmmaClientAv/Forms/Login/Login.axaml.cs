using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using EmmaClientAv.Helpers;
using EmmaClientAv.Services;
using Avalonia.Input;



namespace EmmaClientAv.Forms.Login;

public partial class Login : Window
{
    public Login()
    {
        InitializeComponent();
        
        BtnOk.Click += BtnOk_Click;
        BtnCancel.Click += BtnCancel_Click;

#if DEBUG
        TxtUser.Text = "marco.altomare.1970@gmail.com";
        TxtPassword.Text = "nocafla";
#endif
    }

    async Task<bool> IfLoginSucceded(string user, string password)
    {
        ILoginService loginService = new LoginService(App.Config.ServerUrl, user, password);
        var loginResponse =  await loginService.LoginAsync();
        if (loginResponse is not null){
            if (!string.IsNullOrWhiteSpace(loginResponse.url)){
                App.Config.ServerUrl = loginResponse.url;
                ConfigManager.Save(App.Config);
            }

            return loginResponse.esito;
        }
        else 
        {
            return false;
        }
    }

    private async void BtnOk_Click(object? sender, RoutedEventArgs e)
    {
        // 1. Prendi i dati inseriti dall'utente
        string? username = TxtUser.Text;
        string? password = TxtPassword.Text;

        if ( string.IsNullOrWhiteSpace(username)) {
            await  DialogHelper.ShowErrorDialog(this, "Errore", "Inserire Utente!");
            return;
        }

          if ( string.IsNullOrWhiteSpace(password)) {
            await  DialogHelper.ShowErrorDialog(this, "Errore", "Inserire Password!");
            return;
        }

        try
        {
            this.Cursor = new Cursor(StandardCursorType.Wait);
            // 2. Fai la tua verifica delle credenziali (finta o reale)
            if (await IfLoginSucceded(username, password ?? string.Empty))
            {
                App.CurrentApp.EMMMA_USER = username!;
                App.CurrentApp.EMMMA_PASSWORD = password!;

                // Se il login è corretto, recuperiamo l'ApplicationLifetime corrente
                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // Instanziamo la finestra principale (es. MainWindow)
                    var mainWindow = new MainWindow();
                    mainWindow.WindowState = WindowState.Maximized;
                    // Impostiamo la MainWindow dell'applicazione sulla nuova finestra
                    desktop.MainWindow = mainWindow;

                    // Mostriamo la finestra principale
                    mainWindow.Show();
                }

                // Chiudiamo la finestra di Login attuale
                this.Close();
            }
            else
            {
                await DialogHelper.ShowErrorDialog(this, "Errore", "Credenziali errate !");
                // Errore: credenziali errate (puoi mostrare un messaggio, resettare i campi, ecc.)
                TxtPassword.Text = string.Empty;
            }
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

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        // Se l'utente annulla, chiudiamo l'applicazione
        this.Close();
    }
}