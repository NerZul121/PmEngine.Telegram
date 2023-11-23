using Microsoft.EntityFrameworkCore;
using PmEngine.Core.Entities;
using PmEngine.Core.Interfaces;

namespace PmEngine.Telegram.Entities
{
    [PrimaryKey("TGID", "ChatId")]
    public class TelegramUserEntity : IDataEntity
    {
        public virtual UserEntity Owner { get; set; }
        public long TGID { get; set; }
        public long ChatId { get; set; }
    }
}