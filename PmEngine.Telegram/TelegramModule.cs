using Microsoft.Extensions.DependencyInjection;
using PmEngine.Core.BaseClasses;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Extensions;
using PmEngine.Telegram.Interfaces;

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
            services.AddHostedService<ConfigureWebhook>();

            return services;
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