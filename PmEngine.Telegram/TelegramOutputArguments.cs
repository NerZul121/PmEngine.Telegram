using PmEngine.Core;

namespace PmEngine.Telegram
{
    public class TelegramOutputArguments : ActionArguments
    {
        public int? Theme { get { return Get<int?>("Theme"); } }
    }
}