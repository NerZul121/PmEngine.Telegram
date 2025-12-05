using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PmEngine.Core;
using PmEngine.Core.BaseMarkups;
using PmEngine.Core.Daemons;
using PmEngine.Core.Extensions;
using PmEngine.Core.Interfaces;
using PmEngine.Core.SessionElements;
using PmEngine.Telegram.Args;
using PmEngine.Telegram.Entities;
using PmEngine.Telegram.Interfaces;
using System;
using System.Text.Json;
using Telegram.Bot;

namespace PmEngine.Telegram.Daemons
{
    public class MessagesQueueDaemon : BaseDaemon
    {
        public static int MaxCountPerSec = 30;
        public static Dictionary<long, int> SendedMessages = new();
        private bool _enabled;
        private TelegramOutput _fakeOutput;

        public MessagesQueueDaemon(IServiceProvider services, ILogger<MessagesQueueDaemon> logger, ILogger<TelegramOutput> tgLogger, ITelegramOutputConfigure config) : base(services, logger)
        {
            _enabled = config.UseQueue;
            DelayInSec = 1;
            _fakeOutput = new TelegramOutput(null, tgLogger, _services.GetRequiredService<ITelegramBotClient>(), _services.GetRequiredService<ITelegramOutputConfigure>(), _services);
        }

        public async override Task Work()
        {
            using var ctx = new TelegramContext(_services.GetRequiredService<PmConfig>());
            var messages = await ctx.Set<MessageQueueEntity>().Where(m => m.Status == "Waiting").OrderBy(m => m.Id).Take(MaxCountPerSec).ToArrayAsync().ConfigureAwait(false);

            foreach (var message in messages)
            {
                try
                {
                    var additionals = String.IsNullOrEmpty(message.Arguments) ? new Arguments() : JsonSerializer.Deserialize<Arguments>(message.Arguments) ?? new Arguments();
                    additionals.As<TgOutputAdditionals>().IgnoreQueue = true;
                    INextActionsMarkup? actions = null;

                    if (!string.IsNullOrEmpty(message.Actions))
                    {
                        var saveModel = JsonSerializer.Deserialize<List<List<ActionWrapperSaveModel>>>(message.Actions);
                        actions = new BaseMarkup(saveModel.Select(s => s.Select(b => b.Wrap(_services))));
                    }

                    if (message.ForChatTgId is not null)
                    {
                        message.MessageId = await _fakeOutput.ShowContent(message.Text ?? "", actions, message.Media is null ? null : JsonSerializer.Deserialize<object[]>(message.Media), additionals, message.ForChatTgId).ConfigureAwait(false);
                    }
                    else if (message.ForUserTgId is not null)
                    {
                        var tgUser = await ctx.Set<TelegramUserEntity>().AsNoTracking().Include(t => t.Owner).FirstAsync(u => u.TGID == message.ForUserTgId).ConfigureAwait(false);
                        var us = await _services.GetRequiredService<ServerSession>().GetUserSession(tgUser.Owner.Id).ConfigureAwait(false);
                        message.MessageId = await us.GetOutputOrCreate(typeof(TelegramOutput)).ShowContent(message.Text ?? "", actions, message.Media is null ? null : JsonSerializer.Deserialize<object[]>(message.Media), additionals).ConfigureAwait(false);
                    }

                    message.Status = "Sended";
                    ctx.Remove(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Ошибка отправки сообщения ID {Id}: {ex}", message.Id, ex);
                    message.Status = "Error.\r\n" + ex;
                    message.MessageId = -1;
                }

                message.SendedDate = DateTime.Now;
                SendedMessages[message.Id] = message.MessageId ?? -1;

                await ctx.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}