namespace PmEngine.Telegram
{
    public class TelegramOutputArguments : Core.Arguments
    {
        public int? Theme { get { return Get<int?>("Theme"); } }
    }
}