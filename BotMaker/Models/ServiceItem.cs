namespace BotMaker.Models
{
    public class ServiceItem
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; } = 0;
    }
}
