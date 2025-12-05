using Microsoft.EntityFrameworkCore;
using PmEngine.Core;
using PmEngine.Core.BaseClasses;
using PmEngine.Telegram.Entities;

namespace PmEngine.Telegram
{
    public class TelegramContext : PMEContext
    {
        public DbSet<TelegramUserEntity> TgUserData { get; set; }
        public DbSet<TelegramChatEntity> TgChats { get; set; }
        public DbSet<MessageQueueEntity> TgMessagesQueue { get; set; }

        public TelegramContext(PmConfig config = null) : base(config)
        {
        }
    }
}