using Microsoft.EntityFrameworkCore;
using PmEngine.Core.Interfaces;
using PmEngine.Core;

namespace PmEngine.Telegram
{
    internal class TelegramDbContextRegistrator : IDbContextRegistrator
    {
        public Type[]? DependsOn => null;

        public DbContext CreateContext(PmConfig config, IServiceProvider serviceProvider)
        {
            return new TelegramContext(config);
        }
    }
}