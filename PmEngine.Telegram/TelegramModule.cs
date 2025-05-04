using Microsoft.Extensions.DependencyInjection;
using PmEngine.Core.BaseClasses;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Extensions;
using PmEngine.Telegram.Interfaces;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.AspNetCore.Builder;

namespace PmEngine.Telegram
{
    public class TelegramModule : BaseModuleRegistrator
    {
        public override void AdditionalRegistrate(IServiceCollection services, IEnumerable<Type> allTypes)
        {
            services.AddTransient(typeof(IDataContext), typeof(TelegramContext));
            services.AddScoped(typeof(IOutputManager), typeof(TelegramOutput));
            services.AddScoped(typeof(ITelegramOutput), typeof(TelegramOutput));
        }
    }

    public static class TelegramModuleExt
    {
        public static IServiceCollection AddTelegramModule(this IServiceCollection services, Action<ITelegramOutputConfigure>? conf = null)
        {
            services.AddSingleton<IModuleRegistrator>(new TelegramModule());
            var telegramconf = new TelegramOutputConfigure();

            if (conf is not null)
                conf(telegramconf);

            services.AddSingleton<ITelegramOutputConfigure>(telegramconf);
            services.AddSingleton<IContentRegistrator, TelegramRegistrator>();
            
            return services;
        }

        /*public static async Task SetDefaultTgWebhook(this WebApplication app)
        {
            var botController = new BaseTGController();
            var tgClient = app.Services.GetRequiredService<ITelegramBotClient>();
            var guid = Guid.NewGuid();
            app.MapPost("/bot" + guid, ([FromBody] Update update) => botController.Post(update, app.Services));
            await tgClient.SetWebhook($"{Environment.GetEnvironmentVariable("HOST_URL")}/bot" + guid);
        }

        public static void EnableRecive(this WebApplication app, BaseTGController? controller = null)
        {
            EnableRecive(app.Services, controller);
        }*/

        public static void EnableRecive(this IServiceProvider services, BaseTGController? controller = null)
        {
            if (controller is null)
                controller = new();

            var bot = services.GetRequiredService<ITelegramBotClient>();

            bot.StartReceiving(async (bot, update, token) =>
            {
                Console.WriteLine(JsonSerializer.Serialize(update));
                await controller.Post(update, services);
            },
            async (bot, exception, errorSource, token) =>
            {
                Console.WriteLine(exception);
            });
        }
    }

    public class TelegramRegistrator : IContentRegistrator
    {
        public TelegramRegistrator(IEngineConfigurator config)
        {
            config.Properties.DefaultOutputSetter.Add((user) =>
            {
                return user.TelegramData() is not null ? user.GetOutput<ITelegramOutput>() : null;
            });
        }

        public int Priority { get; set; } = 0;

        public Task Registrate()
        {
            return Task.CompletedTask;
        }
    }
}