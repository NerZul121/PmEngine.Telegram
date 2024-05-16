using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PmEngine.Core.Daemons;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace PmEngine.Telegram
{
    public class TgLongboolService : BaseDaemon
    {
        private UpdateHandler _updateHandler;

        public TgLongboolService(IServiceProvider services, ILogger logger) : base(services, logger)
        {
            DelayInSec = 0;
            _updateHandler = new UpdateHandler(services, logger);
        }

        public override async Task Work()
        {
            var client = _services.GetRequiredService<ITelegramBotClient>();
            await client.ReceiveAsync(_updateHandler);
            await Task.Delay(1);
        }
    }

    public class UpdateHandler : IUpdateHandler
    {
        private BaseTGController _controller = new();
        private IServiceProvider _services;
        private ILogger _logger;

        public UpdateHandler(IServiceProvider services, ILogger logger)
        {
            _controller = new();
            _services = services;
            _logger = logger;
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            return _controller.Post(update, botClient, _logger, _services);
        }
    }
}
