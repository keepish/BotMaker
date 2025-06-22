using BotMaker.Models;
using BotMaker.Services;
using BotMaker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace BotMaker.ViewModels
{
    public partial class InstructionViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        public int ProgressValue => CurrentStepIndex + 1;
        public int ProgressMax => Steps.Count;
        public bool ShowPreviousButton => CurrentStepIndex > 0;
        public bool ShowNextButton => CurrentStepIndex < Steps.Count - 1;

        [ObservableProperty]
        private int _currentStepIndex = 0;

        [ObservableProperty]
        private List<InstructionStep> _steps;

        public InstructionStep CurrentStep =>
        Steps != null && Steps.Count > 0 && CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count
        ? Steps[CurrentStepIndex]
        : null;

        public InstructionViewModel(INavigationService navigation)
        {
            _navigation = navigation;
            InitializeSteps();
        }

        private void InitializeSteps()
        {
            Steps = new List<InstructionStep>
            {
                new ()
                {
                    Number = 1,
                    Title = "Поиск BotFather",
                    Description = "В поисковой строке Telegram введите BotFather, после чего перейдите в чат, обратите внимание, что официальный BotFather помечен галочкой.",
                    ImagePath = "Assets/Steps/step1.png"
                },
                new ()
                {
                    Number = 2,
                    Title = "Запуск BotFather",
                    Description = "Запустите бота командой /start.",
                    ImagePath = "Assets/Steps/step2.png"
                },
                new ()
                {
                    Number = 3,
                    Title = "Создание нового бота",
                    Description = "В меню выберите команду /newbot либо введите вручную.",
                    ImagePath = "Assets/Steps/step3.png"
                },
                new ()
                {
                    Number = 4,
                    Title = "Создание имени бота",
                    Description = "Введите имя вашего бота.",
                    ImagePath = "Assets/Steps/step4.png"
                },
                new ()
                {
                    Number = 5,
                    Title = "Создание уникального тэга бота",
                    Description = "Введите уникальный тэг вашего бота, учтите, что тэг должен заканчиваться словом Bot или bot.",
                    ImagePath = "Assets/Steps/step5.png"
                },
                new ()
                {
                    Number = 6,
                    Title = "Получение ключа API",
                    Description = "",
                    ImagePath = "Assets/Steps/step6.png"
                }
            };

            var UserInfoSteps = new List<InstructionStep>
            {
                new ()
                {
                    Number = 1,
                    Title = "Поиск userinfobot",
                    Description = "В поисковой строке Telegram введите userinfobot, после чего перейдите в чат.",
                    ImagePath = "Assets/Steps/user_info_step1.png"
                },
                new ()
                {
                    Number = 2,
                    Title = "Запуск userinfobot",
                    Description = "Запустите бота командой /start, в ответе вы увидите свой id.",
                    ImagePath = "Assets/Steps/user_info_step2.png"
                },
            };
        }

        partial void OnCurrentStepIndexChanged( int value)
        {
            OnPropertyChanged(nameof(CurrentStep));
            OnPropertyChanged(nameof(ShowPreviousButton));
            OnPropertyChanged(nameof(ShowNextButton));
            OnPropertyChanged(nameof(ProgressValue));
        }

        [RelayCommand]
        private void NextStep()
        {
            if (CurrentStepIndex < Steps.Count - 1)
                CurrentStepIndex++;
        }

        [RelayCommand]
        private void PreviousStep()
        {
            if (CurrentStepIndex > 0)
                CurrentStepIndex--;
        }

        [RelayCommand]
        private void CloseInstruction()
        {
            _navigation.NavigateTo<StartView>();
        }
    }
}
