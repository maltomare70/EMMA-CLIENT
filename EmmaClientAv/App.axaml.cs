using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;
using System.Threading;
namespace EmmaClientAv;

public partial class App : Application
{
    
    // La tua variabile globale
    //public string EMMMA_ENDPOINT { get; set; } = "https://emma-aegc.onrender.com";
    public string EMMMA_ENDPOINT { get; set; } = "http://localhost:9000";
    public string EMMMA_USER { get; set; } = "marco@emma.it";
    public string EMMMA_PASSWORD { get; set; } = "";
    
    // Helper statico per un accesso rapido
    public static App CurrentApp => (App)Application.Current!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
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