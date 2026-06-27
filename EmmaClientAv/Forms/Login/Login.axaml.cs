using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using EmmaClientAv.Helpers;
using EmmaServer.Entities;


namespace EmmaClientAv.Forms.Login;

public partial class Login : Window
{
    public Login()
    {
        InitializeComponent();

        BtnOk.Click += BtnOk_Click;
        BtnCancel.Click += BtnCancel_Click;


        TxtUser.Text = "maltomare70@gmail.com";
        TxtPassword.Text = "nocafla";
    }

    async Task<bool> IfLoginSucceded(string user, string password)
    {
        string urlApi = $"{App.Config.ServerUrl}/api/v1/auth";
        using var client = new HttpClient();
        
        using var request = new HttpRequestMessage(HttpMethod.Post, urlApi);

        // Codifica "username:password" in Base64
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));

        // Aggiungi l'header Authorization nel formato "Basic [Token]"
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        HttpResponseMessage response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
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
        else
        {
            throw new Exception($"Errore durante l'invio: {response.StatusCode} {response.Content}");
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

        try{
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
                    await  DialogHelper.ShowErrorDialog(this, "Errore", "Credenziali errate !");
                    // Errore: credenziali errate (puoi mostrare un messaggio, resettare i campi, ecc.)
                    TxtPassword.Text = string.Empty;
                }
        }
        catch (Exception ex)
        {
            await  DialogHelper.ShowErrorDialog(this, "Errore", ex.Message);
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        // Se l'utente annulla, chiudiamo l'applicazione
        this.Close();
    }
}