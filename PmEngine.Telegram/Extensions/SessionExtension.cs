using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PmEngine.Core;
using PmEngine.Core.Extensions;
using PmEngine.Core.SessionElements;
using PmEngine.Telegram.Entities;

namespace PmEngine.Telegram.Extensions
{
    public static class SessionExtension
    {
        public static long? TGID(this UserSession userSession)
        {
            return userSession.TelegramData()?.TGID;
        }

        public static TelegramUserEntity? TelegramData(this UserSession userSession)
        {
            var tgUser = userSession.GetLocal<TelegramUserEntity?>("tgUserData");
            if (tgUser == null)
            {
                using var context = new TelegramContext(userSession.Services.GetRequiredService<PmConfig>());
                tgUser = context.Set<TelegramUserEntity>().AsNoTracking().Include(u => u.Owner).Where(t => t.Owner.Id == userSession.Id).FirstOrDefault();
                userSession.SetLocal("tgUserData", tgUser);
            }

            return tgUser;
        }
    }
}