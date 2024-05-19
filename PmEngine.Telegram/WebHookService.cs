using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types.Enums;

namespace PmEngine.Telegram
{
    public class ConfigureWebhook : IHostedService
    {
        private readonly ILogger<ConfigureWebhook> _logger;
        private readonly IServiceProvider _services;
        private readonly string _botToken;
        private readonly string _hostUrl;
        public ConfigureWebhook(ILogger<ConfigureWebhook> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _services = serviceProvider;
            _botToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? "";
            _hostUrl = Environment.GetEnvironmentVariable("HOST_URL") ?? "";
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            var webhookAddress = @$"{_hostUrl}/TGBot/{_botToken}";
            _logger.LogInformation($"Setting webhook: {webhookAddress}");
            await botClient.SetWebhookAsync(
                url: webhookAddress,
                allowedUpdates: Array.Empty<UpdateType>(),
                cancellationToken: cancellationToken);

            _logger.LogInformation($"Бот запущен на {_hostUrl}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

            // Remove webhook upon app shutdown
            _logger.LogInformation("Removing webhook");
            await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
        }
    }
}
