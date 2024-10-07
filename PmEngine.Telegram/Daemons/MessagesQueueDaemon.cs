using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PmEngine.Core.BaseMarkups;
using PmEngine.Core.Daemons;
using PmEngine.Core.Extensions;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Entities;
using PmEngine.Telegram.Interfaces;

namespace PmEngine.Telegram.Daemons
{
    public class MessagesQueueDaemon : BaseDaemon
    {
        public static int MaxCountPerSec = 30;
        public static Dictionary<long, int> SendedMessages = new();
        private bool _enabled;

        public MessagesQueueDaemon(IServiceProvider services, ILogger logger, ITelegramOutputConfigure config) : base(services, logger)
        {
            _enabled = config.UseQueue;
            DelayInSec = _enabled ? 1 : 999;
        }

        public async override Task Work()
        {
            if (!_enabled)
                return;

            var messages = await _services.InContext(ctx => ctx.Set<MessageQueueEntity>().AsNoTracking().Where(m => m.Status == "Waiting").OrderBy(m => m.Id).Take(MaxCountPerSec).ToArrayAsync());

            foreach (var message in messages)
            {
                await _services.InContext(async ctx =>
                {
                    ctx.Attach(message);
                    try
                    {
                        var tgUser = await ctx.Set<TelegramUserEntity>().AsNoTracking().Include(t => t.Owner).FirstAsync(u => u.ChatId == message.ForChatId);
                        var us = await _services.GetRequiredService<IServerSession>().GetUserSession(tgUser.Owner.Id);
                        var additionals = String.IsNullOrEmpty(message.Arguments) ? new Core.Arguments() : JsonConvert.DeserializeObject<Core.Arguments>(message.Arguments) ?? new Core.Arguments();
                        additionals.Set("IgnoreQueue", true);
                        INextActionsMarkup? actions = null;
                        if (!string.IsNullOrEmpty(message.Actions))
                        {
                            try
                            {
                                actions = JsonConvert.DeserializeObject<SingleMarkup>(message.Actions);
                            }
                            catch
                            {
                                try
                                {
                                    actions = JsonConvert.DeserializeObject<LinedMarkup>(message.Actions);
                                }
                                catch
                                {
                                    actions = JsonConvert.DeserializeObject<MenueMarkup>(message.Actions);
                                }
                            }

                            message.MessageId = await us.GetOutput<ITelegramOutput>().ShowContent(message.Text ?? "", actions, message.Media is null ? null : JsonConvert.DeserializeObject<object[]>(message.Media), additionals);
                            message.Status = "Sended";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Ошибка отправки сообщения ID {Id}: {ex}", message.Id, ex);
                        message.Status = "Error.\r\n" + ex;
                        message.MessageId = -1;
                    }

                    message.SendedDate = DateTime.Now;
                    SendedMessages[message.Id] = message.MessageId ?? -1;

                    await ctx.SaveChangesAsync();
                });
            }
        }
    }
}