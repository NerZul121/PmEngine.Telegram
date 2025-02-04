using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PmEngine.Core.BaseMarkups;
using PmEngine.Core.Daemons;
using PmEngine.Core.Extensions;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Entities;
using PmEngine.Telegram.Interfaces;
using System.Text.Json;
using Telegram.Bot;

namespace PmEngine.Telegram.Daemons
{
    public class MessagesQueueDaemon : BaseDaemon
    {
        public static int MaxCountPerSec = 30;
        public static Dictionary<long, int> SendedMessages = new();
        private bool _enabled;
        private ITelegramOutput _fakeOutput;

        public MessagesQueueDaemon(IServiceProvider services, ILogger logger, ITelegramOutputConfigure config) : base(services, logger)
        {
            _enabled = config.UseQueue;
            DelayInSec = _enabled ? 1 : 999;
            _fakeOutput = new TelegramOutput(_logger, _services.GetRequiredService<ITelegramBotClient>(), null, _services.GetRequiredService<ITelegramOutputConfigure>(), _services);
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
                        var additionals = String.IsNullOrEmpty(message.Arguments) ? new Core.Arguments() : JsonSerializer.Deserialize<Core.Arguments>(message.Arguments) ?? new Core.Arguments();
                        additionals.Set("IgnoreQueue", true);
                        INextActionsMarkup? actions = null;
                        if (!string.IsNullOrEmpty(message.Actions))
                        {
                            try
                            {
                                actions = JsonSerializer.Deserialize<SingleMarkup>(message.Actions);
                            }
                            catch
                            {
                                try
                                {
                                    actions = JsonSerializer.Deserialize<LinedMarkup>(message.Actions);
                                }
                                catch
                                {
                                    actions = JsonSerializer.Deserialize<MenueMarkup>(message.Actions);
                                }
                            }

                            if (message.ForChatTgId is not null)
                            {
                                message.MessageId = await _fakeOutput.ShowContent(message.Text ?? "", actions, message.Media is null ? null : JsonSerializer.Deserialize<object[]>(message.Media), additionals, message.ForChatTgId);
                            }
                            if (message.ForUserTgId is not null)
                            {
                                var tgUser = await ctx.Set<TelegramUserEntity>().AsNoTracking().Include(t => t.Owner).FirstAsync(u => u.TGID == message.ForUserTgId);
                                var us = await _services.GetRequiredService<IServerSession>().GetUserSession(tgUser.Owner.Id);
                                message.MessageId = await us.GetOutput<ITelegramOutput>().ShowContent(message.Text ?? "", actions, message.Media is null ? null : JsonSerializer.Deserialize<object[]>(message.Media), additionals);
                            }

                            message.Status = "Sended";
                            ctx.Remove(message);
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