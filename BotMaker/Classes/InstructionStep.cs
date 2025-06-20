using Avalonia.Media;

namespace BotMaker.Classes
{
    public class InstructionStep
    {
        public int Number { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IImage ImagePath { get; set; }
    }
}
