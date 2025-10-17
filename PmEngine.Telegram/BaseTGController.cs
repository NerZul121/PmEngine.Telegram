using Microsoft.Extensions.DependencyInjection;
using PmEngine.Core.Entities;
using PmEngine.Core.Enums;
using PmEngine.Core.Interfaces;
using PmEngine.Core;
using PmEngine.Telegram.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PmEngine.Telegram.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PmEngine.Telegram.Encoders;
using PmEngine.Telegram.Entities;
using PmEngine.Core.Extensions;
using PmEngine.Telegram.Args;

namespace PmEngine.Telegram
{
    public class BaseTGController
    {
        public virtual async Task<bool> Post(Update update, IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<ITelegramBotClient>();
            var logger = serviceProvider.GetRequiredService<ILogger>();

            var msg = update.Message;
            TelegramUserEntity? tgUser = null;
            UserEntity? user = null;
            IUserSession? session;

            try
            {
                tgUser = await GetOrCreateUser(update, logger, serviceProvider);

                user = tgUser?.Owner;

                if (tgUser is null || user is null)
                    return false;

                session = await serviceProvider.GetRequiredService<ServerSession>().GetUserSession(user.Id, null, typeof(TelegramOutput));

                TaskRunner.Run(async () =>
                {
                    try
                    {
                        await UserProcess(update, session, client, logger, serviceProvider);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Ошибка обработки апдейта {update.Id}: {ex}");
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            return false;
        }

        public virtual void UserRightsVerify(IUserSession user)
        {
            if (user.CachedData.UserType == (int)UserType.Banned)
                throw new Exception("Sorry, your account has blocked :(");
        }

        public virtual async Task<TelegramUserEntity> GetOrCreateUser(Update update, ILogger logger, IServiceProvider serviceProvider, long? userId = null)
        {
            TelegramUserEntity? tgUser = null;

            var from = update.Message?.From ?? update.CallbackQuery?.From;
            if (from is null)
                throw new Exception("Пользователь не определен.");

            var chat = update.Message?.Chat;

            await serviceProvider.GetRequiredService<IContextHelper>().InContext(async (context) =>
            {
                tgUser = await context.Set<TelegramUserEntity>().AsNoTracking().Include(u => u.Owner).FirstOrDefaultAsync(p => p.TGID == from.Id);

                if (chat != null && chat.Id != from.Id)
                {
                    var existChat = await context.Set<TelegramChatEntity>().FirstOrDefaultAsync(c => c.ChatId == chat.Id);
                    if (existChat is null)
                    {
                        existChat = new() { ChatId = chat.Id };
                        context.Add(existChat);
                    }

                    existChat.ChatTitle = chat.Title;
                    existChat.ChannelLogin = chat.Username;

                    await context.SaveChangesAsync();
                }

                if (tgUser is null)
                {
                    tgUser = new TelegramUserEntity() { TGID = from.Id, Login = from.Username, FirstName = from.FirstName, LastName = from.LastName };

                    if (userId is not null)
                        tgUser.Owner = context.Set<UserEntity>().First(u => u.Id == userId);
                    else
                        tgUser.Owner = new() { RegistrationDate = DateTime.Now, LastOnlineDate = DateTime.Now };

                    await context.Set<TelegramUserEntity>().AddAsync(tgUser);

                    await context.SaveChangesAsync();
                    return;
                }
                else
                {
                    tgUser.FirstName = from.FirstName;
                    tgUser.LastName = from.LastName;
                    tgUser.Login = from.Username;
                    await context.SaveChangesAsync();
                }
            });

            return tgUser;
        }

        public virtual async Task<bool> UserProcess(Update update, IUserSession session, ITelegramBotClient client, ILogger logger, IServiceProvider serviceProvider)
        {
            var skip = false;
            foreach (var s in serviceProvider.GetServices<ITgCustomLogic>())
            {
                logger.LogInformation($"CustomLogic before: {s.GetType()} - processing");
                if (await s.BeforeProcessUpdate(update, session, client, logger, serviceProvider))
                {
                    logger.LogInformation($"CustomLogic: {s.GetType()} - OK. Exist.");
                    skip = true;
                }
            }

            if (skip)
                return true;

            var msg = update.Message;
            UserRightsVerify(session);

            try
            {
                if (update.CallbackQuery != null && !String.IsNullOrEmpty(update.CallbackQuery.Data))
                {
                    await InlineButtonProcess(update, session, client, logger, serviceProvider);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"INLINE error: {ex}");
                return false;
            }

            if (msg is null || msg.From is null)
                return false;

            logger.LogInformation($"New message from {msg.Chat.Id}: {msg.Text}");

            session.GetOutputOrCreate(typeof(TelegramOutput));

            var stringed = session.NextActions is not null ? session.NextActions.NumeredDuplicates().GetFloatNextActions() : Enumerable.Empty<ActionWrapper>();

            var processor = serviceProvider.GetRequiredService<IEngineProcessor>();

            if (msg.Photo != null && msg.Photo.Any())
            {
                var fileUid = msg.Photo.Last().FileId;

                if (session.InputAction != null)
                {
                    session.InputAction.Arguments.As<TgArguments>().FileUID = fileUid;
                    await processor.ActionProcess(session.InputAction, session);

                    return true;
                }
            }

            if (msg.Document != null)
            {
                var fileUid = msg.Document.FileId;

                if (session.InputAction != null)
                {
                    session.InputAction.Arguments.As<TgArguments>().FileUID = fileUid;
                    await processor.ActionProcess(session.InputAction, session);

                    return true;
                }
            }

            if (String.IsNullOrEmpty(msg.Text))
            {
                foreach (var s in serviceProvider.GetServices<ITgCustomLogic>())
                {
                    logger.LogInformation($"CustomLogic after: {s.GetType()} - processing");
                    if (await s.AfterProcessUpdate(update, session, client, logger, serviceProvider))
                    {
                        logger.LogInformation($"CustomLogic: {s.GetType()} - OK. Exist.");
                        return true;
                    }
                }

                return false;
            }

            var text = serviceProvider.GetRequiredService<ITelegramOutputConfigure>().ParseInputEntities ? Tg.TextWithEntities(msg.Text, msg.Entities) : msg.Text;

            var act = stringed.FirstOrDefault(a => a.DisplayName == text);

            if (act is not null)
            {
                act.Arguments.As<TgArguments>().ImputMessageId = msg.Id;
                act.Arguments.As<TgArguments>().Update = update;
                await processor.ActionProcess(act, session);
                return true;
            }
            else if (text.StartsWith("/"))
            {
                var cmdmngr = serviceProvider.GetRequiredService<CommandManager>();
                await cmdmngr.DoCommand(text, session);
                return true;
            }
            else if (session.InputAction != null)
            {
                session.InputAction.Arguments.As<TgArguments>().ImputMessageId = msg.Id;
                session.InputAction.Arguments.As<TgArguments>().Update = update;
                session.InputAction.Arguments.InputData = text;
                await processor.ActionProcess(session.InputAction, session);
                return true;
            }

            foreach (var s in serviceProvider.GetServices<ITgCustomLogic>())
            {
                logger.LogInformation($"CustomLogic after: {s.GetType()} - processing");
                await s.AfterProcessUpdate(update, session, client, logger, serviceProvider);
            }

            return false;
        }

        public virtual async Task InlineButtonProcess(Update update, IUserSession session, ITelegramBotClient client, ILogger logger, IServiceProvider serviceProvider)
        {
            var callbackQuery = update.CallbackQuery ?? throw new Exception("Пустой CallbackQuery");
            var chatId = update.CallbackQuery.Message?.Chat.Id ?? 0;

            logger.LogInformation($"New callback from {chatId}: {callbackQuery}");

            if (session is null)
                return;

            var processor = serviceProvider.GetRequiredService<IEngineProcessor>();

            var messageId = update.CallbackQuery.Message.Id;

            var action = session.NextActions?.GetFloatNextActions().FirstOrDefault(a => a.GUID == update.CallbackQuery.Data);
            if (action is null)
                return;

            /*var model = update.CallbackQuery.Data.GetInLineModel();

            if (model is null)
                return;

            var wrapper = model.ToWrapper(serviceProvider);
            if (wrapper is null)
                return;

            if (wrapper.ActionType is null)
                return;*/

            var msgActType = action.Arguments.As<TgArguments>().MessageActionId;
            if (msgActType is null)
                msgActType = (int)serviceProvider.GetRequiredService<ITelegramOutputConfigure>().DefaultInLineMessageAction;

            if (msgActType == 1)
                await session.Output.DeleteMessage(messageId);

            if (msgActType == 2)
            {
                var btn = callbackQuery.Message.ReplyMarkup.InlineKeyboard.SelectMany(s => s).First(b => b.CallbackData == callbackQuery.Data);
                await client.EditMessageText(callbackQuery.Message.Chat.Id, messageId, $"{callbackQuery.Message.Text}\r\n\r\n{btn.Text}");
            }

            if (msgActType == 3)
            {
                var btn = callbackQuery.Message.ReplyMarkup.InlineKeyboard.SelectMany(s => s).First(b => b.CallbackData == callbackQuery.Data);
                session.SetLocal("tryupdatechatid", callbackQuery.Message.Chat.Id);
                session.SetLocal("tryupdatemessageid", messageId);
            }

            action.Arguments.As<TgArguments>().ImputMessageId = messageId;
            action.Arguments.As<TgArguments>().CallbackQuery = callbackQuery;

            if (action.ActionType is not null || !String.IsNullOrEmpty(action.ActionTypeName))
                await processor.ActionProcess(action, session);

            await client.AnswerCallbackQuery(update.CallbackQuery.Id, action.Arguments.As<TgArguments>().CallbackText, action.Arguments.As<TgArguments>().CallbackAlert, action.Arguments.As<TgArguments>().CallbackURL);
        }

        public static WebAppAuthData GetWebAppAuthDataFromString(string data)
        {
            return JsonSerializer.Deserialize<WebAppAuthData>(data);
        }

        public static bool TryAuth(string data)
        {
            return TryAuth(GetWebAppAuthDataFromString(data));
        }

        public static bool TryAuth(WebAppAuthData data)
        {
            try
            {
                var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("BOT_TOKEN")));

                var options = new JsonSerializerOptions()
                {
                    Encoder = new EmojiEncoder()
                };

                var stringeddata = $"auth_date={data.auth_date}\nquery_id={data.query_id}\nuser={JsonSerializer.Serialize(data.user, options)}";

                var generatedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(stringeddata));

                var actualHash = Convert.FromHexString(data.hash);

                return actualHash.SequenceEqual(generatedHash);
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}