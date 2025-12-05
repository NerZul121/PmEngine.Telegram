using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace PmEngine.Telegram
{
    public class SendMessageModel
    {
        public SendMessageModel(Update update, ITelegramBotClient bot)
        {
            if (update?.Message == null)
                return;

            if (update.Message.VideoNote is not null)
            {
                _sendF = (chatId, markup, threadId) => bot.SendVideoNote(chatId, new InputFileId(update.Message.VideoNote.FileId), replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (update.Message.Audio is not null)
            {
                _sendF = (chatId, markup, threadId) => bot.SendAudio(chatId, new InputFileId(update.Message.Audio.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (update.Message.Animation is not null)
            {
                _sendF = (chatid, markup, threadId) => bot.SendAnimation(chatid, new InputFileId(update.Message.Animation.FileId), caption: update.Message.Caption, replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (update.Message.Contact is not null)
            {
                _sendF = (chatid, markup, threadId) => bot.SendContact(chatid, update.Message.Contact.PhoneNumber, update.Message.Contact.FirstName, replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (update.Message.Voice is not null)
            {
                _sendF = (chatId, markup, threadId) => bot.SendAudio(chatId, new InputFileId(update.Message.Voice.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (update.Message.Photo is not null)
            {
                _sendF = (chatId, markup, threadId) => bot.SendPhoto(chatId, new InputFileId(update.Message.Photo.OrderBy(p => p.FileSize).Last().FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (update.Message.Video is not null)
            {
                _sendF = (chatId, markup, threadId) => bot.SendVideo(chatId, new InputFileId(update.Message.Video.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (update.Message.Document is not null)
            {
                _sendF = (chatId, markup, threadId) => bot.SendDocument(chatId, new InputFileId(update.Message.Document.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup);
                return;
            }

            if (update.Message.Location is not null)
            {
                _sendF = (chatId, markup, threadId) => bot.SendLocation(chatId, update.Message.Location.Latitude, update.Message.Location.Latitude, replyMarkup: markup, messageThreadId: threadId);
                return;
            }

            if (!String.IsNullOrEmpty(update.Message.Text))
            {
                _sendF = (chatId, markup, threadId) => bot.SendMessage(chatId, AddEntitites(update.Message.Text, update.Message.Entities) ?? "pozor", entities: update.Message.Entities, parseMode: ParseMode.Html, replyMarkup: markup, messageThreadId: threadId);
                return;
            }
        }

        private string? AddEntitites(string? text, MessageEntity[]? entities)
        {
            return Tg.TextWithEntities(text, entities);
        }

        private Func<long, ReplyMarkup?, int?, Task<Message>> _sendF;

        public async Task<int> Send(long chatId, ReplyMarkup? markup = null, int? threadId = null)
        {
            return (await _sendF(chatId, markup, threadId).ConfigureAwait(false)).Id;
        }
    }
}