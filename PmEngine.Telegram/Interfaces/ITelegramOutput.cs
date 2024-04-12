using PmEngine.Core.Interfaces;

namespace PmEngine.Telegram.Interfaces
{
    public interface ITelegramOutput : IOutputManager
    {
        public string Storage { get; set; }

        public Task<bool> CheckUserInTheGroup(long userId, long? chatId = null);
        public Task DeleteMessage(int messageId, long? chatId = null);
        public Task EditContent(int messageId, string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null, long? chatId = null);
        public Task PinMessage(int messageId, bool pin = true, long? chatId = null);
        public Task<int> ShowContent(string content, INextActionsMarkup? nextActions = null, IEnumerable<object>? media = null, Core.Arguments? additionals = null, long? chatId = null);
    }
}