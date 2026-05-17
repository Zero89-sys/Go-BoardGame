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

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.MainContent.Content = new LoadingScreen();

            Task.Run(async () =>
            {
                await StartBot();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow.MainContent.Content = new MenuView();
                });
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

        GtpService.Instance.StartEngine(exe, model, cfg);

        for (int i = 0; i < 10; i++)
        {
            var response = await GtpService.Instance.SendCommand("name");
            if (!string.IsNullOrEmpty(response))
                return;

            await Task.Delay(500);
        }

        throw new Exception("KataGo se nepodařilo spustit");
    }
}
