using BotMaker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BotMaker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private object? _currentView;

    [RelayCommand]
    private void OpenInstruction()
    {
        CurrentView = new InstructionView { DataContext = new InstructionViewModel() };
    }
}
