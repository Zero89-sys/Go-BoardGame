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
using System.Threading.Tasks;

namespace Gogame;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            mainWindow.MainContent.Content = new LoadingScreen();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

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
            singleViewPlatform.MainView = new GameView
            {
                DataContext = new MainViewModel()
            };
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
}
