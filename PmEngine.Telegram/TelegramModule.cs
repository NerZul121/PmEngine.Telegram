using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PmEngine.Core;
using PmEngine.Core.BaseClasses;
using PmEngine.Core.Extensions;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Interfaces;
using System.Text.Json;
using Telegram.Bot;

namespace PmEngine.Telegram
{
    public class TelegramModule : BasePMEngineModule
    {
        public override void AdditionalRegistrate(IServiceCollection services, IEnumerable<Type> allTypes)
        {
            services.AddSingleton<IDbContextRegistrator, TelegramDbContextRegistrator>();
        }
    }

    public static class TelegramModuleExt
    {
        public static IServiceCollection AddTelegramModule(this IServiceCollection services, Action<ITelegramOutputConfigure>? conf = null)
        {
            services.AddPmModule<TelegramModule>();
            var telegramconf = new TelegramOutputConfigure();

            if (conf is not null)
                conf(telegramconf);

            services.AddSingleton<ITelegramOutputConfigure>(telegramconf);

            return services;
        }        

        public static void EnableRecive(this IServiceProvider services, BaseTGController? controller = null)
        {
            if (controller is null)
                controller = new();

            var bot = services.GetRequiredService<ITelegramBotClient>();

            bot.StartReceiving(async (bot, update, token) =>
            {
                Console.WriteLine(JsonSerializer.Serialize(update));
                await controller.Post(update, services).ConfigureAwait(false);
            },
            async (bot, exception, errorSource, token) =>
            {
                Console.WriteLine(exception);
            });
        }
    }
}