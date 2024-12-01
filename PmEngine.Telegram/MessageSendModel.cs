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
                _sendF = (chatId, markup) => bot.SendVideoNoteAsync(chatId, new InputFileId(update.Message.VideoNote.FileId), replyMarkup: markup);
                return;
            }

            if (update.Message.Audio is not null)
            {
                _sendF = (chatId, markup) => bot.SendAudioAsync(chatId, new InputFileId(update.Message.Audio.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup);
                return;
            }

            if (update.Message.Animation is not null)
            {
                _sendF = (chatid, markup) => bot.SendAnimationAsync(chatid, new InputFileId(update.Message.Animation.FileId), caption: update.Message.Caption, replyMarkup: markup);
                return;
            }

            if (update.Message.Contact is not null)
            {
                _sendF = (chatid, markup) => bot.SendContactAsync(chatid, update.Message.Contact.PhoneNumber, update.Message.Contact.FirstName, replyMarkup: markup);
                return;
            }

            if (update.Message.Voice is not null)
            {
                _sendF = (chatId, markup) => bot.SendAudioAsync(chatId, new InputFileId(update.Message.Voice.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup);
                return;
            }

            if (update.Message.Photo is not null)
            {
                _sendF = (chatId, markup) => bot.SendPhotoAsync(chatId, new InputFileId(update.Message.Photo.OrderBy(p => p.FileSize).Last().FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup);
                return;
            }

            if (update.Message.Video is not null)
            {
                _sendF = (chatId, markup) => bot.SendVideoAsync(chatId, new InputFileId(update.Message.Video.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup);
                return;
            }

            if (update.Message.Document is not null)
            {
                _sendF = (chatId, markup) => bot.SendDocumentAsync(chatId, new InputFileId(update.Message.Document.FileId), caption: AddEntitites(update.Message.Caption, update.Message.CaptionEntities), parseMode: ParseMode.Html, replyMarkup: markup);
                return;
            }

            if (update.Message.Location is not null)
            {
                _sendF = (chatId, markup) => bot.SendLocationAsync(chatId, update.Message.Location.Latitude, update.Message.Location.Latitude, replyMarkup: markup);
                return;
            }

            if (update.Message.Poll is not null)
            {
                _sendF = (chatId, markup) => bot.SendPollAsync(chatId, update.Message.Poll.Question, update.Message.Poll.Options.Select(o => new InputPollOption(o.Text)), type: update.Message.Poll.Type.ToLower() == "quiz" ? PollType.Quiz : PollType.Regular, isAnonymous: update.Message.Poll.IsAnonymous, allowsMultipleAnswers: update.Message.Poll.AllowsMultipleAnswers, correctOptionId: update.Message.Poll.CorrectOptionId, replyMarkup: markup);
                return;
            }

            if (!String.IsNullOrEmpty(update.Message.Text))
            {
                _sendF = (chatId, markup) => bot.SendTextMessageAsync(chatId, AddEntitites(update.Message.Text, update.Message.Entities) ?? "pozor", entities: update.Message.Entities, parseMode: ParseMode.Html, replyMarkup: markup);
                return;
            }
        }

        private string? AddEntitites(string? text, MessageEntity[]? entities)
        {
            return Tg.TextWithEntities(text, entities);
        }

        private Func<long, IReplyMarkup?, Task<Message>> _sendF;

        public async Task<int> Send(long chatId, IReplyMarkup? markup = null)
        {
            return (await _sendF(chatId, markup)).MessageId;
        }
    }
}