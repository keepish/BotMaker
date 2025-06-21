using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.IO;

namespace BotMaker.Classes
{
    public class InstructionStep
    {
        public int Number { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IImage Image { get; private set; }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                LoadImage();
            }
        }

        private string _imagePath;

        private void LoadImage()
        {
            try
            {
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _imagePath);
                if (File.Exists(fullPath))
                {
                    Image = new Bitmap(fullPath);
                }
                else
                {
                    var resourceUri = $"avares://BotMaker/{_imagePath.Replace("\\", "/")}";
                    Image = new Bitmap(resourceUri);
                }
            }
            catch
            {
                Image = new Bitmap("avares://BotMaker/Assets/1.png");
            }
        }
    }
}
