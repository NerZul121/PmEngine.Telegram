using Microsoft.Extensions.Logging;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Interfaces;
using Telegram.Bot;

namespace PmEngine.Telegram
{
    public class TelegramOutputFactory : IOutputManagerFactory
    {
        private readonly ILogger _logger;
        private readonly ITelegramBotClient _botClient;
        private readonly ITelegramOutputConfigure _config;
        private readonly IServiceProvider _serviceProvider;

        public TelegramOutputFactory(ILogger logger, ITelegramBotClient client, ITelegramOutputConfigure config, IServiceProvider services)
        {
            _logger = logger;
            _botClient = client;
            _config = config;
            _serviceProvider = services;
        }

        public Type OutputType => typeof(TelegramOutput);

        public IOutputManager CreateForUser(IUserSession user)
        {
            return new TelegramOutput(user, _logger, _botClient, _config, _serviceProvider);
        }
    }
}