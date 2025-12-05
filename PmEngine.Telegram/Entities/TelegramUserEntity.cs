using Microsoft.EntityFrameworkCore;
using PmEngine.Core.Entities;

namespace PmEngine.Telegram.Entities
{
    [PrimaryKey("TGID")]
    public class TelegramUserEntity
    {
        public virtual UserEntity Owner { get; set; }
        public long TGID { get; set; }
        public string? Login { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Comment { get; set; }
    }
}