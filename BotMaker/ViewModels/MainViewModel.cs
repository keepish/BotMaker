using BotMaker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BotMaker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    public MainViewModel()
    {
        CurrentView = new StartView { DataContext = this };
    }

    [RelayCommand]
    private void OpenInstruction()
    {
        CurrentView = new InstructionView { DataContext = new InstructionViewModel() };
    }

    [RelayCommand]
    private void OpenStartView()
    { }
}
