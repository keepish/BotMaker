using BotMaker.ServiceLayer.Services;
using BotMaker.Services;
using BotMaker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace BotMaker.ViewModels
{
    public partial class StartViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly UserService _userService = new();

        public StartViewModel(INavigationService navigation)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        [ObservableProperty]
        private long? _telegramUserId = null;

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
                if (TelegramUserId != null)
                {
                    var user = await _userService.GetUserByIdAsync(TelegramUserId.Value);

                    if (user == null)
                    {
                        ErrorMessage = "UserId неверен или не зарегистрирован";
                        return;
                    }

                    CurrentUserService.CurrentUser = user;
                    _navigation.NavigateTo<CreateBotView>();
                }
                else
                {
                    ErrorMessage = "Заполните поле ввода Телеграм user_id";
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [RelayCommand]
        public async Task Registration()
        {
            var url = "https://t.me/BusinessPointBot";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        [RelayCommand]
        private void OpenUserIdInstruction()
        {
            _navigation.NavigateTo<InstructionView>();
        }
    }
}
