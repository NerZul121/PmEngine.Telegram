using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PmEngine.Core;
using PmEngine.Core.Extensions;
using PmEngine.Core.Interfaces;
using PmEngine.Core.SessionElements;
using PmEngine.Core.SessionElements;
using PmEngine.Telegram.Args;
using PmEngine.Telegram.Daemons;
using PmEngine.Telegram.Entities;
using PmEngine.Telegram.Extensions;
using PmEngine.Telegram.Interfaces;
using PmEngine.Telegram.Models;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PmEngine.Telegram
{
    public class TelegramOutput : IOutputManager
    {
        public string Storage { get; set; } = "";

        private ILogger _logger;
        private ITelegramBotClient _client { get; set; }
        private bool _useQueue;

        private UserSession? _user { get; set; }
        private IServiceProvider _services;

        private long? _tgid;

        /// <summary>
        /// Инициализация аутпата
        /// </summary>
        /// <param name="logger">логгер</param>
        /// <param name="client">телеграммный клиент</param>
        public TelegramOutput(UserSession? user, ILogger<TelegramOutput> logger, ITelegramBotClient client, ITelegramOutputConfigure config, IServiceProvider services)
        {
            _logger = logger;
            _client = client;
            _useQueue = config.UseQueue;
            _services = services;
            _user = user;
            _tgid = _user?.TGID();
        }

        public async Task PinMessage(int messageId, bool pin = true, long? chatId = null)
        {
            if (chatId == null)
                chatId = _tgid;

            if (pin)
                await _client.PinChatMessage(chatId, messageId).ConfigureAwait(false);
            else
                await _client.UnpinChatMessage(chatId, messageId).ConfigureAwait(false);
        }

        /// <summary>
        /// Проверка на то, что пользователь состоит в группе
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<bool> CheckUserInTheGroup(long chatId, long? userId = null)
        {
            if (userId == null)
                userId = _tgid;

            try
            {
                var kakish = await _client.GetChatMember(chatId, userId.Value).ConfigureAwait(false);
                _logger.LogInformation($"Проверка пользователя {userId} в {chatId}. Результат: {kakish.Status}");
                return kakish.Status != ChatMemberStatus.Left && kakish.Status != ChatMemberStatus.Kicked;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка проверки пользователя userId:{userId}, chatId:{chatId}, error: {ex}");
                return false;
            }
        }

        public async Task<int> ShowContent(string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Arguments? additionals = null, long? chatId = null)
        {
            var tgAddionals = additionals?.As<TgOutputAdditionals>();

            if (chatId is null)
            {
                chatId = _tgid;

                if (!string.IsNullOrEmpty(content))
                    foreach (var tr in _services.GetServices<IOutputMutation>())
                        content = await tr.Mutate(content, _user).ConfigureAwait(false);
            }

            if (_useQueue && tgAddionals is not null && !tgAddionals.IgnoreQueue)
            {
                var message = new MessageQueueEntity()
                {
                    Text = content,
                    Actions = nextActions is null ? null : JsonSerializer.Serialize(nextActions.GetNextActions().Select(s => s.Select(a => new ActionWrapperSaveModel(a)).ToList()).ToList()),
                    Media = media is null ? null : JsonSerializer.Serialize(media),
                    Arguments = additionals is null ? null : JsonSerializer.Serialize(additionals)
                };

                message.ForChatTgId = chatId;

                using (var ctx = new TelegramContext(_services.GetRequiredService<PmConfig>()))
                {
                    ctx.Add(message);
                    await ctx.SaveChangesAsync().ConfigureAwait(false);
                }

                int id = 0;
                var timeout = TimeSpan.FromSeconds(30); // Таймаут ожидания сообщения
                var startTime = DateTime.UtcNow;

                while (!MessagesQueueDaemon.SendedMessages.TryGetValue(message.Id, out id))
                {
                    if (DateTime.UtcNow - startTime > timeout)
                    {
                        _logger.LogWarning($"Таймаут ожидания сообщения из очереди. MessageId: {message.Id}");
                        MessagesQueueDaemon.SendedMessages.Remove(message.Id, out _);
                        return -1;
                    }
                    await Task.Delay(33);
                }

                MessagesQueueDaemon.SendedMessages.Remove(message.Id, out _);

                return id;
            }

            var replyMarkup = GetReplyMarkup(nextActions);

            if (!String.IsNullOrEmpty(content) && content.StartsWith("{"))
            {
                try
                {
                    var update = JsonSerializer.Deserialize<Update>(content);
                    if (update is not null)
                    {
                        var model = new SendMessageModel(update, _client);
                        return await model.Send(chatId.Value, replyMarkup).ConfigureAwait(false);
                    }
                }
                catch { }
            }

            var messageId = -1;

            var theme = tgAddionals?.Theme;

            if (media is null || !media.Any())
            {
                if (_user is not null)
                {
                    var tryupdatechatid = _user.GetLocal<long?>("tryupdatechatid");
                    var userTgId = _user.TGID();

                    if (tryupdatechatid is not null && chatId == userTgId)
                    {
                        try
                        {
                            var tryupdatemessageid = Convert.ToInt32(_user.GetLocal<int?>("tryupdatemessageid"));
                            await _client.EditMessageText(tryupdatechatid, Convert.ToInt32(tryupdatemessageid), content, ParseMode.Html, replyMarkup: (InlineKeyboardMarkup)replyMarkup).ConfigureAwait(false);
                            return tryupdatemessageid;
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                _logger.LogWarning(ex.ToString());
                                var tryupdatemessageid = Convert.ToInt32(_user.GetLocal<int?>("tryupdatemessageid"));
                                await _client.DeleteMessage(tryupdatechatid, tryupdatemessageid).ConfigureAwait(false);
                            }
                            catch (Exception ex2)
                            {
                                _logger.LogError(ex2.ToString());
                            }
                        }

                        _user.SetLocal("tryupdatechatid", null);
                        _user.SetLocal("tryupdatemessageid", null);
                    }
                }

                messageId = (await _client.SendMessage(chatId, content, replyMarkup: replyMarkup, messageThreadId: theme, parseMode: ParseMode.Html).ConfigureAwait(false)).MessageId;
                return messageId;
            }

            if (media.Count() == 1)
                await InFileStream(media.First().ToString(), async (fs) =>
                {
                    var format = media.First().ToString().ToLower();

                    if (format.EndsWith("|v") || format.EndsWith(".mp4") || format.EndsWith(".gif"))
                        messageId = (await _client.SendVideo(chatId, fs, replyMarkup: replyMarkup, caption: content, messageThreadId: theme, parseMode: ParseMode.Html).ConfigureAwait(false)).MessageId;
                    else
                        messageId = (await _client.SendPhoto(chatId, fs, replyMarkup: replyMarkup, caption: content, messageThreadId: theme, parseMode: ParseMode.Html).ConfigureAwait(false)).MessageId;

                    return messageId;
                }).ConfigureAwait(false);
            else
            {
                var files = media.Select(m => m.ToString()).Select(GetInputFile).Where(f => f is not null).ToList();

                var streams = new List<Stream>();

                foreach (var smed in media)
                {
                    if (smed is null || smed.ToString().StartsWith("fileUID") || smed.ToString().StartsWith("http"))
                        continue;

                    var stream = new MemoryStream(Convert.FromBase64String(smed.ToString()));
                    streams.Add(stream);
                    var photo = new InputMediaPhoto(new InputFileStream(stream, Guid.NewGuid().ToString() + ".jpg"));
                    files.Add(photo);
                }

                if (files.Any())
                    messageId = (await _client.SendMediaGroup(chatId, files).ConfigureAwait(false)).Last().MessageId;

                if (!String.IsNullOrEmpty(content))
                    messageId = (await _client.SendMessage(chatId, content, replyMarkup: replyMarkup, messageThreadId: theme, parseMode: ParseMode.Html).ConfigureAwait(false)).MessageId;

                foreach (var str in streams)
                {
                    str.Close();
                    str.Dispose();
                }
            }

            return messageId;
        }

        private IAlbumInputMedia? GetInputFile(string s)
        {
            if (s.StartsWith("fileUID"))
                return GetFileByUid(s);

            if (s.StartsWith("http"))
                return new InputMediaPhoto(new InputFileUrl(s));

            return null;
        }

        private IAlbumInputMedia GetFileByUid(string s)
        {
            var uid = s.Replace("fileUID:", "").Split('|').First();
            var type = s.Split('|').Last().ToLower();

            switch (type)
            {
                case "v":
                    return new InputMediaVideo(new InputFileId(uid));
                case "d":
                    return new InputMediaDocument(new InputFileId(uid));
                default:
                    return new InputMediaPhoto(new InputFileId(uid));
            }
        }

        private static ReplyMarkup? GetReplyMarkup(INextActionsMarkup? nextActions)
        {
            if (nextActions != null)
            {
                if (nextActions.GetFloatNextActions().Any())
                {
                    if (nextActions.InLine)
                        return new InlineKeyboardMarkup(nextActions.GetNextActions().Select(s => s.Where(a => a.Visible).Select(a => a is InlineButtonActionWrapper inb ? inb.Button : (a is WebAppActionWrapper ? InlineKeyboardButton.WithWebApp(a.DisplayName, new WebAppInfo() { Url = ((WebAppActionWrapper)a).Url }) : (a is UrlActionWrapper ? InlineKeyboardButton.WithUrl(a.DisplayName, ((UrlActionWrapper)a).Url) : InlineKeyboardButton.WithCallbackData(a.DisplayName, a.GUID))))));
                    else
                        return new ReplyKeyboardMarkup(nextActions.GetNextActions().Select(s => s.Where(a => a.Visible).Select(a => GetButton(a)))) { ResizeKeyboard = true };
                }
                else
                    return new ReplyKeyboardRemove();
            }

            return null;
        }

        private static KeyboardButton GetButton(ActionWrapper a)
        {
            return new KeyboardButton(a.DisplayName)
            {
                WebApp = a is WebAppActionWrapper ? new WebAppInfo() { Url = ((WebAppActionWrapper)a).Url } : null,
                RequestContact = a.Arguments.Get<bool>("tg_RequestContact"),
                RequestLocation = a.Arguments.Get<bool>("tg_RequestLocation")
            };
        }

        public async Task EditContent(int messageId, string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Arguments? additionals = null, long? chatId = null)
        {
            if (chatId == null)
            {
                chatId = _user.TGID();

                if (!string.IsNullOrEmpty(content))
                    foreach (var tr in _services.GetServices<IOutputMutation>())
                        content = await tr.Mutate(content, _user).ConfigureAwait(false);
            }

            InlineKeyboardMarkup? replyMarkup = null;
            var theme = additionals?.As<TgOutputAdditionals>().Theme;

            if (nextActions != null)
            {
                if (nextActions.InLine)
                {
                    replyMarkup = new InlineKeyboardMarkup(nextActions.GetNextActions().Select(s => s.Where(a => a.Visible).Select(a => a is InlineButtonActionWrapper ilba ? ilba.Button : (a is WebAppActionWrapper ? InlineKeyboardButton.WithWebApp(a.DisplayName, new WebAppInfo() { Url = ((WebAppActionWrapper)a).Url }) : (a is UrlActionWrapper ? InlineKeyboardButton.WithUrl(a.DisplayName, ((UrlActionWrapper)a).Url) : InlineKeyboardButton.WithCallbackData(a.DisplayName, a.GUID))))));
                    await _client.EditMessageText(chatId, messageId, content, replyMarkup: replyMarkup, parseMode: ParseMode.Html).ConfigureAwait(false);
                    return;
                }
            }

            await _client.EditMessageText(chatId, messageId, content, parseMode: ParseMode.Html).ConfigureAwait(false);
        }

        public async Task DeleteMessage(int messageId, long? chatId = null)
        {
            if (chatId == null)
                chatId = _tgid;

            await _client.DeleteMessage(chatId, messageId).ConfigureAwait(false);
        }

        /// <summary>
        /// Получение файлстрима из строки. Принмиает Base64 и путь к файлу
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task InFileStream(string file, Func<InputFile, Task<int>> action)
        {
            if (file.StartsWith("fileUID:"))
            {
                var uid = file.Replace("fileUID:", "").Split('|').First();
                await action(new InputFileId(uid));
                return;
            }
            else if (file.StartsWith("http"))
            {
                await action(new InputFileUrl(file));
                return;
            }
            try
            {
                if (file.Contains("."))
                    using (var stream = new FileStream(Storage + file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        await action(new InputFileStream(stream, file.Split('\\').Last())).ConfigureAwait(false);
                else
                    using (var stream = new MemoryStream(Convert.FromBase64String(file)))
                        await action(new InputFileStream(stream)).ConfigureAwait(false);
            }
            catch (DirectoryNotFoundException)
            {
                using (var stream = new FileStream(Storage + "/NotFound.jpg", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    await action(new InputFileStream(stream, file.Split('\\').Last())).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                using (var stream = new FileStream(Storage + "/NotFound.jpg", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    await action(new InputFileStream(stream, file.Split('\\').Last())).ConfigureAwait(false);
            }
        }

        public Task<int> ShowContent(string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null)
        {
            return ShowContent(content, nextActions, media, additionals, _tgid);
        }

        public Task EditContent(int messageId, string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null)
        {
            return EditContent(messageId, content, nextActions, media, additionals, _tgid);
        }

        public Task DeleteMessage(int messageId)
        {
            return DeleteMessage(messageId, _tgid);
        }

        public async Task<int> Send(SendMessageModel model, INextActionsMarkup? nextActions = null)
        {
            var chatId = _tgid;
            if (chatId is null)
                return -1;

            return await model.Send(chatId.Value, GetReplyMarkup(nextActions)).ConfigureAwait(false);
        }

        public async void ShowContentSafe(string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Arguments? additionals = null)
        {
            try
            {
                await ShowContent(content, nextActions, media, additionals).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
            }
        }

        public async void EditContentSafe(int messageId, string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Arguments? additionals = null)
        {
            try
            {
                await EditContent(messageId, content, nextActions, media, additionals).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
            }
        }

        public async void DeleteMessageSafe(int messageId)
        {
            try
            {
                await DeleteMessage(messageId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
            }
        }
    }
}