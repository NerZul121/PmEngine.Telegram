using PmEngine.Telegram.Enums;
using PmEngine.Telegram.Interfaces;

namespace PmEngine.Telegram
{
    internal class TelegramOutputConfigure : ITelegramOutputConfigure
    {
        public string? ApiURL { get; set; }
        public MessageActionType DefaultInLineMessageAction { get; set; } = new();
    }
}
