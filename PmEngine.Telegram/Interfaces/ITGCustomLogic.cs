using Microsoft.Extensions.Logging;
using PmEngine.Core.Interfaces;
using PmEngine.Core.SessionElements;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PmEngine.Telegram.Interfaces
{
    /// <summary>
    /// Custom processing for tg updates
    /// </summary>
    public interface ITgCustomLogic
    {
        /// <summary>
        /// Custom processing tg uipdate before main process. If return true - skip all next processes.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="session"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public Task<bool> BeforeProcessUpdate(Update update, UserSession session, ITelegramBotClient client, ILogger logger, IServiceProvider serviceProvider);

        /// <summary>
        /// Custom processing tg uipdate after main process. If return true - skip all next processes.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="session"></param>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public Task<bool> AfterProcessUpdate(Update update, UserSession session, ITelegramBotClient client, ILogger logger, IServiceProvider serviceProvider);
    }
}