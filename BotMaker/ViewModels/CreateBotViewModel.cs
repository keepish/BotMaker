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

        private readonly BotsService _botService;
        private readonly ScriptGenerator _scriptGenerator;

        [ObservableProperty]
        private string? _token = "";

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
        private bool _botTracksExpenses = false;

        [ObservableProperty]
        private bool _trackOrdersInTable = false;

        [ObservableProperty]
        private bool _addSearchFilter = false;

        [ObservableProperty]
        private bool _addNotifications = false;

        public CreateBotViewModel(INavigationService navigation)
        {
            _botService = new BotsService();
            _scriptGenerator = new ScriptGenerator();
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
            {
                // Путь к базе sqlite (пример)
                var dbPath = Path.Combine(targetFolder, "bot_db.sqlite3");

                try
                {
                    using var connection = new System.Data.SQLite.SQLiteConnection($"Data Source={dbPath};Version=3;");
                    connection.Open();

                    using var transaction = connection.BeginTransaction();
                    if (AddFAQ)
                    {
                        // Создаем таблицу faq, если не существует
                        using (var createFaqCmd = new System.Data.SQLite.SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS faq (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    question TEXT NOT NULL,
    answer TEXT NOT NULL
);", connection, transaction))
                        {
                            createFaqCmd.ExecuteNonQuery();
                        }
                        // Вставляем FAQ
                        using var faqCmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO faq (question, answer) VALUES (@q, @a)", connection, transaction);
                        var qParam = faqCmd.Parameters.Add("@q", System.Data.DbType.String);
                        var aParam = faqCmd.Parameters.Add("@a", System.Data.DbType.String);

                        foreach (var faqItem in FAQ)
                        {
                            qParam.Value = faqItem.Question;
                            aParam.Value = faqItem.Answer;
                            faqCmd.ExecuteNonQuery();
                        }

                    }

                    if (ClientsCanMakeOrders)
                    {
                        // Создаем таблицу products, если не существует
                        using (var createProductsCmd = new System.Data.SQLite.SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    price REAL NOT NULL
);", connection, transaction))
                        {
                            createProductsCmd.ExecuteNonQuery();
                        }

                        // Вставляем Products (Services)
                        using var prodCmd = new System.Data.SQLite.SQLiteCommand("INSERT INTO products (name, description, price) VALUES (@n, @d, @p)", connection, transaction);
                        var nParam = prodCmd.Parameters.Add("@n", System.Data.DbType.String);
                        var dParam = prodCmd.Parameters.Add("@d", System.Data.DbType.String);
                        var pParam = prodCmd.Parameters.Add("@p", System.Data.DbType.Decimal);

                        foreach (var service in Services)
                        {
                            nParam.Value = service.Name;
                            dParam.Value = service.Description ?? string.Empty;
                            pParam.Value = service.Price;
                            prodCmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    await MessageBox($"Ошибка при заполнении базы данных: {ex.Message}", window);
                    return;
                }

            }

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
            return _scriptGenerator.GetScript(Token, CurrentUserService.CurrentUser.UserId, CompanyName, AddFAQ, ClientsCanMakeOrders, BotTracksExpenses, TrackOrdersInTable, AddSearchFilter, AddNotifications);
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
        private void OpenApiKeyInstruction()
        {
            CurrentUserService.CurrentInstruction = "api";
            _navigation.NavigateTo<InstructionView>();
        }
    }
}
