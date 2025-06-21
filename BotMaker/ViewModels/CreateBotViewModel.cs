using Avalonia.Controls;
using BotMaker.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace BotMaker.ViewModels
{
    public partial class CreateBotViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string? _telegramUserId = null;

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
        public async Task GenerateScript(Window window)
        {
            var exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var scriptFolderPath = Path.Combine(exeFolder, "BotTest");
            var scriptFilePath = Path.Combine(scriptFolderPath, "bot.py");
            File.WriteAllText(scriptFilePath, GenerateScriptContent());
            var dlg = new OpenFolderDialog();
            var targetFolder = await dlg.ShowAsync(window);

            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                Console.WriteLine("Папка не выбрана");
                return;
            }

            string folderName = new DirectoryInfo(scriptFolderPath).Name;
            targetFolder = Path.Combine(targetFolder, folderName + $"_{CompanyName}");

            CopyDirectory(scriptFolderPath, targetFolder);
            File.WriteAllText(scriptFilePath, string.Empty);
            Console.WriteLine("Ваш чат-бот успешно создан!");
        }

        private string GenerateScriptContent()
        {
            return $@"from aiogram import Bot, Dispatcher, types
from aiogram.filters import Command
from aiogram.enums import ParseMode
import asyncio

API_TOKEN = '8071173139:AAFEQwQbH92MhM0zy_otQh4uZI4PzApQ4-Y'

bot = Bot(token=API_TOKEN)
dp = Dispatcher()

@dp.message(Command(commands=['start', 'help']))
async def send_welcome(message: types.Message):
    await message.reply(
        ""Здравствуйте! 👋\n""
        f""Добро пожаловать в компанию '{CompanyName}'! Мы рады видеть вас здесь. Чем можем помочь?""
    )

@dp.message()
async def echo(message: types.Message):
    await message.answer(""Спасибо за ваше сообщение! Мы свяжемся с вами в ближайшее время."")

async def main():
    await dp.start_polling(bot)

if __name__ == ""__main__"":
    asyncio.run(main())
";
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
    }
}
