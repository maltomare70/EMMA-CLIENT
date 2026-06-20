using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;
using System.Threading;
using EmmaClientAv.Forms.Login;
using EmmaClientAv.Helpers;

namespace EmmaClientAv;

public partial class App : Application
{
    public string EMMMA_USER { get; set; } = "";
    public string EMMMA_PASSWORD { get; set; } = "";
    

    public static AppConfig Config { get; private set; } = null!;
    
    // Helper statico per un accesso rapido
    public static App CurrentApp => (App)Application.Current!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

        // Carica la configurazione all'avvio
        Config = ConfigManager.Load();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //desktop.MainWindow = new MainWindow();
            desktop.MainWindow = new Login();
        }
        
        // Forza la cultura in Italiano (imposta formati e testi del DatePicker)
        var culture = new CultureInfo("it-IT");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        
        // // Dice ad Avalonia di aggiornare i testi dei suoi controlli interni
        // AppBuilder.Configure<App>()
        //     .With(new Win32PlatformOptions { }) // o la piattaforma che usi
        //     .LogToTrace();

        base.OnFrameworkInitializationCompleted();
    }
}