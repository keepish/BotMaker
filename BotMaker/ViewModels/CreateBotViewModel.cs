using Avalonia.Controls;
using BotMaker.Models;
using BotMaker.ServiceLayer.Services;
using BotMaker.Services;
using BotMaker.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLayer.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace BotMaker.ViewModels
{
    public partial class CreateBotViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;

        private readonly UserService _userService;
        private readonly BotsService _botService;

        [ObservableProperty]
        private string? _token = "8071173139:AAFEQwQbH92MhM0zy_otQh4uZI4PzApQ4-Y";

        [ObservableProperty]
        private string _companyName = "";

        [ObservableProperty]
        private bool _addFAQ = false;

        [ObservableProperty]
        private string _question = "";

        [ObservableProperty]
        private string _answer = "";
        public ObservableCollection<FAQItem> FAQ { get; set; } = new();


        [ObservableProperty]
        private bool _clientsCanMakeOrders = false;

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _description = "";

        [ObservableProperty]
        private decimal _price = 0;

        public ObservableCollection<ServiceItem> Services { get; set; } = new();


        [ObservableProperty]
        private bool _botKeepsClientBase = false;

        [ObservableProperty]
        private bool _trackOrdersInTable = false;

        [ObservableProperty]
        private bool _addSearchFilter = false;

        [ObservableProperty]
        private bool _addNotifications = false;

        public CreateBotViewModel(INavigationService navigation)
        {
            _userService = new UserService();
            _botService = new BotsService();
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        [RelayCommand]
        public void AddFAQItem()
        {
            FAQ.Add(new FAQItem() { Answer = Answer, Question = Question });
        }

        [RelayCommand]
        public void AddService()
        {
            Services.Add(new ServiceItem() { Name = Name, Description = Description, Price = Price });
        }

        [RelayCommand]
        public void RemoveService(ServiceItem service)
        {
            if (Services.Contains(service))
                Services.Remove(service);
        }

        [RelayCommand]
        public void RemoveFAQItem(FAQItem fAQItem)
        {
            if (FAQ.Contains(fAQItem))
                FAQ.Remove(fAQItem);
        }

        [RelayCommand]
        public async Task StartGenerateBot(Window window)
        {
            if (!CurrentUserService.CurrentUser.IsVip && CurrentUserService.CurrentUser.Bots.Count > 3)
            {
                MessageBox("Лимит ботов превышен!", window);
                return;
            }

            var exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptFolderPath = Path.Combine(exeFolder, "BotTest");
            var scriptFilePath = Path.Combine(scriptFolderPath, "bot.py");
            File.WriteAllText(scriptFilePath, GenerateScriptContent());
            var dlg = new OpenFolderDialog();
            var targetFolder = await dlg.ShowAsync(window);

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                MessageBox("Папка не выбрана", window);
                return;
            }

            string folderName = new DirectoryInfo(scriptFolderPath).Name;
            targetFolder = Path.Combine(targetFolder, folderName + $"_{CompanyName}");

            CopyDirectory(scriptFolderPath, targetFolder);
            File.WriteAllText(scriptFilePath, string.Empty);
            var result = await _botService.AddBotAsync(CurrentUserService.CurrentUser.UserId, Token, "Bot_" + CompanyName);

            if (result == true)
                MessageBox("Ваш чат-бот успешно создан!", window);
            else
                MessageBox("Бот с таким именем уже зарегестрирован за вами", window);
        }

        private async Task MessageBox(string message, Window window)
        {
            var dialog = new Window
            {
                Title = "Информация",
                Width = 300,
                Height = 150,
                Content = new TextBlock
                {
                    Text = message,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };

            await dialog.ShowDialog(window);
        }

        private string GenerateScriptContent()
        {
            return "";
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(dir, destDir);
            }
        }

        [RelayCommand]
        public void OpenUserIdInstruction()
        {

        }

        [RelayCommand]
        private void OpenApiKeyInstruction()
        {
            _navigation.NavigateTo<InstructionView>();
        }
    }
}
