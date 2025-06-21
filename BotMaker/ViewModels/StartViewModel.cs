using BotMaker.Services;
using BotMaker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace BotMaker.ViewModels
{
    public partial class StartViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;

        public StartViewModel(INavigationService navigation)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        [ObservableProperty]
        private string? _token = "8071173139:AAFEQwQbH92MhM0zy_otQh4uZI4PzApQ4-Y";

        [ObservableProperty]
        private string? _errorMessage = "";

        [RelayCommand]
        private void OpenApiKeyInstruction()
        {
            _navigation.NavigateTo<InstructionView>();
        }

        [RelayCommand]
        public async Task OpenCreateBotViewAsync()
        {
            try
            {
                var botClient = new TelegramBotClient(Token);
                var me = await botClient.GetMe();
                _navigation.NavigateTo<CreateBotView>();
            }
            catch (Exception ex) when (ex is ApiRequestException || ex is ArgumentException)
            {
                ErrorMessage = "Токен неверен или бот не существует";
            }
        }
    }
}
