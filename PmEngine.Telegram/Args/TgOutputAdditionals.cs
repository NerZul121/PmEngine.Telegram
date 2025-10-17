using PmEngine.Core;

namespace PmEngine.Telegram.Args
{
    public class TgOutputAdditionals : ArgumentsAccessor
    {
        public bool IgnoreQueue { get { return Source.Get<bool>(nameof(IgnoreQueue)); } set { Source.Set(nameof(IgnoreQueue), value); } }
        public int Theme { get { return Source.Get<int>(nameof(Theme)); } set { Source.Set(nameof(Theme), value); } }
        public long TryUpdateChatId { get { return Source.Get<long>(nameof(TryUpdateChatId)); } set { Source.Set(nameof(TryUpdateChatId), value); } }
        public int TryUpdateMessageId { get { return Source.Get<int>(nameof(TryUpdateMessageId)); } set { Source.Set(nameof(TryUpdateMessageId), value); } }
    }
}