using PmEngine.Telegram.Enums;

namespace PmEngine.Telegram.Interfaces
{
    public interface ITelegramOutputConfigure
    {
        public string? ApiURL { get; set; }
        public MessageActionType DefaultInLineMessageAction { get; set; }
        public bool ParseInputEntities { get; set; }
    }
}
