using Microsoft.Extensions.Logging;
using PmEngine.Core;
using PmEngine.Core.Extensions;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Daemons;
using PmEngine.Telegram.Entities;
using PmEngine.Telegram.Extensions;
using PmEngine.Telegram.Interfaces;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PmEngine.Telegram
{
    public class TelegramOutput : ITelegramOutput
    {
        public string Storage { get; set; } = "";

        private ILogger _logger;
        private ITelegramBotClient _client { get; set; }

        private IUserScopeData _userData;
        private bool _useQueue;

        private IServiceProvider _services;

        /// <summary>
        /// Инициализация аутпата
        /// </summary>
        /// <param name="logger">логгер</param>
        /// <param name="client">телеграммный клиент</param>
        public TelegramOutput(ILogger logger, ITelegramBotClient client, IUserScopeData userData, ITelegramOutputConfigure config, IServiceProvider services)
        {
            _logger = logger;
            _client = client;
            _userData = userData;
            _useQueue = config.UseQueue;
            _services = services;
        }

        public async Task PinMessage(int messageId, bool pin = true, long? chatId = null)
        {
            if (chatId == null)
                chatId = _userData.Owner.TGID();

            if (pin)
                await _client.PinChatMessage(chatId, messageId);
            else
                await _client.UnpinChatMessage(chatId, messageId);
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
                userId = _userData.Owner.CachedData.Id;

            try
            {
                var kakish = await _client.GetChatMember(chatId, userId.Value);
                _logger.LogInformation($"Проверка пользователя {userId} в {chatId}. Результат: {kakish.Status}");
                return kakish.Status != ChatMemberStatus.Left && kakish.Status != ChatMemberStatus.Kicked;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка проверки пользователя userId:{userId}, chatId:{chatId}, error: {ex}");
                return false;
            }
        }

        public async Task<int> ShowContent(string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null, long? chatId = null)
        {
            if (_useQueue && additionals is not null && !additionals.Get<bool>("IgnoreQueue"))
            {
                var message = new MessageQueueEntity()
                {
                    Text = content,
                    Actions = nextActions is null ? null : JsonSerializer.Serialize(nextActions),
                    Media = media is null ? null : JsonSerializer.Serialize(media),
                    Arguments = additionals is null ? null : JsonSerializer.Serialize(additionals)
                };

                if (chatId is null)
                    message.ForUserTgId = _userData.Owner.TGID();
                else
                    message.ForChatTgId = chatId;

                await _services.InContext(async ctx =>
                {
                    ctx.Add(message);
                    await ctx.SaveChangesAsync();
                });

                int id = 0;

                while (!MessagesQueueDaemon.SendedMessages.TryGetValue(message.Id, out id))
                    await Task.Delay(33);

                MessagesQueueDaemon.SendedMessages.Remove(message.Id, out _);

                return id;
            }

            if (chatId == null)
                chatId = _userData.Owner.TGID();

            var replyMarkup = GetReplyMarkup(nextActions);

            // Если тут json, то пробуем через update
            if (!String.IsNullOrEmpty(content) && content.StartsWith("{"))
            {
                try
                {
                    var update = System.Text.Json.JsonSerializer.Deserialize<Update>(content);
                    if (update is not null)
                    {
                        var model = new SendMessageModel(update, _client);
                        return await model.Send(chatId.Value, replyMarkup);
                    }
                }
                catch { }
            }

            var messageId = -1;

            var theme = additionals?.Get<int?>("Theme");

            if (media is null || !media.Any())
            {
                messageId = (await _client.SendMessage(chatId, content, replyMarkup: replyMarkup, messageThreadId: theme, parseMode: ParseMode.Html)).MessageId;
                return messageId;
            }

            if (media.Count() == 1)
                await InFileStream(media.First().ToString(), async (fs) => messageId = (await _client.SendPhoto(chatId, fs, replyMarkup: replyMarkup, caption: content, messageThreadId: theme, parseMode: ParseMode.Html)).MessageId);
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
                    messageId = (await _client.SendMediaGroup(chatId, files)).Last().MessageId;

                if (!String.IsNullOrEmpty(content))
                    messageId = (await _client.SendMessage(chatId, content, replyMarkup: replyMarkup, messageThreadId: theme, parseMode: ParseMode.Html)).MessageId;

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

        private static IReplyMarkup? GetReplyMarkup(INextActionsMarkup? nextActions)
        {
            if (nextActions != null)
            {
                if (nextActions.GetFloatNextActions().Any())
                {
                    if (nextActions.InLine)
                        return new InlineKeyboardMarkup(nextActions.GetNextActions().Select(s => s.Where(a => a.Visible).Select(a => a is WebAppActionWrapper ? InlineKeyboardButton.WithWebApp(a.DisplayName, new WebAppInfo() { Url = ((WebAppActionWrapper)a).Url }) : (a is UrlActionWrapper ? InlineKeyboardButton.WithUrl(a.DisplayName, ((UrlActionWrapper)a).Url) : InlineKeyboardButton.WithCallbackData(a.DisplayName, a.GUID)))));
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

        public async Task EditContent(int messageId, string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null, long? chatId = null)
        {
            if (chatId == null)
                chatId = _userData.Owner.TGID();

            InlineKeyboardMarkup? replyMarkup = null;
            var theme = additionals?.Get<int?>("Theme");

            if (nextActions != null)
            {
                if (nextActions.InLine)
                {
                    replyMarkup = new InlineKeyboardMarkup(nextActions.GetNextActions().Select(s => s.Where(a => a.Visible).Select(a => a is WebAppActionWrapper ? InlineKeyboardButton.WithWebApp(a.DisplayName, new WebAppInfo() { Url = ((WebAppActionWrapper)a).Url }) : (a is UrlActionWrapper ? InlineKeyboardButton.WithUrl(a.DisplayName, ((UrlActionWrapper)a).Url) : InlineKeyboardButton.WithCallbackData(a.DisplayName, a.GUID)))));
                    await _client.EditMessageText(chatId, messageId, content, replyMarkup: replyMarkup, parseMode: ParseMode.Html);
                    return;
                }
            }

            await _client.EditMessageText(chatId, messageId, content, parseMode: ParseMode.Html);
        }

        public async Task DeleteMessage(int messageId, long? chatId = null)
        {
            if (chatId == null)
                chatId = _userData.Owner.TGID();

            await _client.DeleteMessage(chatId, messageId);
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
                var uid = file.Replace("fileUID:", "");
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
                        await action(new InputFileStream(stream, file.Split('\\').Last()));
                else
                    using (var stream = new MemoryStream(Convert.FromBase64String(file)))
                        await action(new InputFileStream(stream));
            }
            catch (DirectoryNotFoundException)
            {
                try
                {
                    using (var stream = new FileStream(Storage + "/NotFound.jpg", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        await action(new InputFileStream(stream, file.Split('\\').Last()));
                }
                catch
                {
                    using (var stream = new MemoryStream(Convert.FromBase64String(_b404)))
                        await action(new InputFileStream(stream));
                }
            }
            catch (FileNotFoundException)
            {
                using (var stream = new FileStream(Storage + "/NotFound.jpg", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    await action(new InputFileStream(stream, file.Split('\\').Last()));
            }
        }

        public Task<int> ShowContent(string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null)
        {
            return ShowContent(content, nextActions, media, additionals, _userData.Owner.TGID());
        }

        public Task EditContent(int messageId, string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null)
        {
            return EditContent(messageId, content, nextActions, media, additionals, _userData.Owner.TGID());
        }

        public Task DeleteMessage(int messageId)
        {
            return DeleteMessage(messageId, _userData.Owner.TGID());
        }

        public async Task<int> Send(SendMessageModel model, INextActionsMarkup? nextActions = null)
        {
            var chatId = _userData.Owner.TGID();
            if (chatId is null)
                return -1;

            return await model.Send(chatId.Value, GetReplyMarkup(nextActions));
        }

        #region 404
        public const string _b404 = "/9j/4AAQSkZJRgABAQEAYABgAAD/4QBoRXhpZgAATU0AKgAAAAgABAEaAAUAAAABAAAAPgEbAAUAAAABAAAARgEoAAMAAAABAAIAAAExAAIAAAARAAAATgAAAAAAAABgAAAAAQAAAGAAAAABcGFpbnQubmV0IDQuMy4xMgAA/9sAQwABAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEB/9sAQwEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEB/8AAEQgCAAIAAwEhAAIRAQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRCkaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX29/j5+v/aAAwDAQACEQMRAD8A/v4ooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACigAooAKKACvBv2nP2mfgp+x38CviH+0j+0R42tPh78IPhdpEOr+LfE13aX+ovCt7qNno2kaZpulaVbXmq6zrmva5qWm6HoekabaXF7qWq6haWkERaXKgH4s/s1f8HQn/BKP9qb47fDT9nfwB4z+NGifEH4v+OdB+G/w9fxr8Gtc07QNe8Z+K9Ut9D8L6Q2p6Je+IpdK/t3WLyz0+1v9ZstP06zluo5tXu9NtEnuIf6H6AOX8a+N/Bvw28JeIvH3xD8V+HPAvgfwjpN5rvirxh4u1rTvDvhjw5ounxGa+1bXNc1e4tNN0vTrSJTJcXl7cwwRKMu44r+aj45f8Hd3/BH34PeJNR8OeFvEf7QP7RA02RYJtf+Bvwhtf8AhG57lWKXMGnal8Y/GPwfbU0tZFKtqFhbXGkXi4n0rUNQtnSZgD6L/Yn/AODln/glH+3F480n4VeEvi74q+CPxP8AEt/FpvhHwX+0l4Tt/hufFt/NGGisNE8aaVrvi/4ZDVrm4aPT9N0HVPHGma/rupSxWWgaXqs8iqf30oAK4b4m/EvwH8Gfh145+LfxS8UaX4J+G/w18Ka9448deL9bleHSfDfhTwxptxq+uazfPFHLM0Fhp9pPOYraGe6nKLBawT3EkUTgH87PhH/g7S/4I6eL/H+k+A4PiF8cNITW/ENh4dsPGOs/AvxSvhR5dSvo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9682pa1q93fatqdzdahe3NxKAep0UAf57H/Bdf8A4Ijf8FL/ANrv/gsprn7Sv7O/7NFz8SvgZ40T9nV7fx9bfE34OeHbCzbwJ4O8HeGPFsWsaV4v+IHh/wARac2lXGgXcuZtHKahamF9Ke+kcwr/AKE9ABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAFFABRQAUUAf/2Q==";
        #endregion
    }
}