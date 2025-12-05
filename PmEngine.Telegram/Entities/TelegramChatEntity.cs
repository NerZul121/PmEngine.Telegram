using Microsoft.EntityFrameworkCore;

namespace PmEngine.Telegram.Entities
{
    [PrimaryKey("ChatId")]
    public class TelegramChatEntity
    {
        public long ChatId { get; set; }
        public string? ChatTitle { get; set; }
        public string? ChannelLogin { get; set; }
        public string? Comment { get; set; }
    }
}