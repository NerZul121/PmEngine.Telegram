using PmEngine.Core;
using Telegram.Bot.Types.ReplyMarkups;

namespace PmEngine.Telegram.Models
{
    public class InlineButtonActionWrapper : ActionWrapper
    {
        public InlineKeyboardButton Button;

        public InlineButtonActionWrapper(InlineKeyboardButton button) : base("")
        {
            Button = button;
        }
    }
}