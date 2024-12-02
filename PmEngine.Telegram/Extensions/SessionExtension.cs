using Microsoft.EntityFrameworkCore;
using PmEngine.Core.Extensions;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Entities;

namespace PmEngine.Telegram.Extensions
{
    public static class SessionExtension
    {
        public static long? TGID(this IUserSession userSession)
        {
            return userSession.TelegramData()?.TGID;
        }

        public static TelegramUserEntity? TelegramData(this IUserSession userSession)
        {
            var tgUser = userSession.GetLocal<TelegramUserEntity?>("tgUserData");
            if (tgUser == null)
            {
                userSession.Services.InContext(async (context) =>
                {
                    tgUser = context.Set<TelegramUserEntity>().AsNoTracking().Include(u => u.Owner).Where(t => t.Owner.Id == userSession.Id).FirstOrDefault();
                }).Wait();

                userSession.SetLocal("tgUserData", tgUser);
            }

            return tgUser;
        }
    }
}