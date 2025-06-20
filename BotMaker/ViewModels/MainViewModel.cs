using BotMaker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BotMaker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [RelayCommand]
    private void OpenInstruction()
    {
        var instructionView = new InstructionView
        {
            DataContext = new InstructionViewModel()
        };
    }
}
