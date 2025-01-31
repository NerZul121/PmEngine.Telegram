using PmEngine.Telegram.Enums;
using PmEngine.Telegram.Interfaces;

namespace PmEngine.Telegram
{
    public class TelegramOutputConfigure : ITelegramOutputConfigure
    {
        public string? ApiURL { get; set; }
        public MessageActionType DefaultInLineMessageAction { get; set; } = MessageActionType.Default;
        public bool ParseInputEntities { get; set; } = true;
        public bool UseQueue { get; set; } = false;
    }
}
