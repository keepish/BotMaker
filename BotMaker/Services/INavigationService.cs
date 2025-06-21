using Avalonia.Controls;

namespace BotMaker.Services
{
    public interface INavigationService
    {
        void NavigateTo<TView>() where TView : Control, new();
        void GoBack();
    }
}
