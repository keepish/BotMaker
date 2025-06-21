using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BotMaker.Services;
using BotMaker.ViewModels;
using BotMaker.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace BotMaker;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        var services = new ServiceCollection();

        services.AddTransient<StartViewModel>();
        services.AddTransient<CreateBotViewModel>();
        services.AddTransient<InstructionViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;

            var contentControl = mainWindow.FindControl<ContentControl>("MainContent")
                ?? throw new InvalidOperationException("ContentControl 'MainContent' not found");

            var navService = new NavigationService(contentControl);
            services.AddSingleton<INavigationService>(_ => navService);

            Services = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(Services);

            navService.NavigateTo<StartView>();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            var mainView = new MainView();
            singleViewPlatform.MainView = mainView;

            var contentControl = mainView.FindControl<ContentControl>("MainContent")
                ?? throw new InvalidOperationException("ContentControl 'MainContent' not found");

            var navService = new NavigationService(contentControl);
            services.AddSingleton<INavigationService>(_ => navService);

            Services = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(Services);

            navService.NavigateTo<StartView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
