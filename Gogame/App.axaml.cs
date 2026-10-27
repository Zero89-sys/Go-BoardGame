using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Gogame.Models;
using Gogame.ViewModels;
using Gogame.Views;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gogame;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static ApiClient Api { get; private set; } = null!;
    public static UserSession Session { get; private set; } = null!;

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var baseUri = new Uri("https://localhost:7109");
        var cookies = new CookieContainer();
        var storage = new CookieStorage();
        storage.Load(cookies, baseUri); // restore saved cookies

        var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true };
        var http = new HttpClient(handler) { BaseAddress = baseUri };
        Api = new ApiClient(http, cookies, storage, baseUri);
        Session = new UserSession();

        _ = RestoreSessionAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            mainWindow.MainContent.Content = new LoadingScreen();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            Navigator.RootContent = mainWindow.MainContent;

            Task.Run(async () =>
            {
                try
                {
                    await StartBot();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error starting the bot: {ex.Message}");
                }
                finally
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        mainWindow.MainContent.Content = new MenuView();
                    });
                }
            });
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var root = new ContentControl
            {
                DataContext = new MainViewModel(),
                Content = new MenuView()
            };
            singleViewPlatform.MainView = root;

            Navigator.RootContent = root;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartBot()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string exe = Path.Combine(baseDir, "Engine", "katago.exe");
        string model = Path.Combine(baseDir, "Engine", "KataGo.txt.gz");
        string cfg = Path.Combine(baseDir, "Engine", "default_gtp.cfg");

        bool isStarted = await GtpService.Instance.StartEngineAsync(exe, model, cfg);

        if (!isStarted)
        {
            System.Diagnostics.Debug.WriteLine("[App] The bot was not started; proceeding without it.");
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            var response = await GtpService.Instance.SendCommand("name");
            if (!string.IsNullOrEmpty(response))
                return;

            await Task.Delay(500);
        }

        throw new Exception("Failed to start KataGo");
        GtpService.Instance.StopEngine();
    }

    // Session restoration
    private static async Task RestoreSessionAsync()
    {
        try
        {
            var result = await Api.GetProfileAsync();

            if (result.Success && result.Data != null)
            {
                Session.SignIn(result.Data, Api);
                Api.SaveSession(); // saves the renewd cookie
            }
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
