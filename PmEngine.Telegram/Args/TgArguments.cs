using PmEngine.Core;
using Telegram.Bot.Types;

namespace PmEngine.Telegram.Args
{
    public class TgArguments : ArgumentsAccessor
    {
        public long InLineArgument { get { return Source.Get<long>(nameof(InLineArgument)); } set { Source.Set(nameof(InLineArgument), value); } }
        public int? MessageActionId { get { return Source.Get<int?>(nameof(MessageActionId)); } set { Source.Set(nameof(MessageActionId), value); } }

        public int ImputMessageId { get { return Source.Get<int>(nameof(ImputMessageId)); } set { Source.Set(nameof(ImputMessageId), value); } }
        public string? FileUID { get { return Source.Get<string?>(nameof(FileUID)); } set { Source.Set(nameof(FileUID), value); } }

        public CallbackQuery? CallbackQuery { get { return Source.Get<CallbackQuery?>(nameof(CallbackQuery)); } set { Source.Set(nameof(CallbackQuery), value); } }
        public Update? Update { get { return Source.Get<Update?>(nameof(Update)); } set { Source.Set(nameof(Update), value); } }

        public string? CallbackText { get { return Source.Get<string?>(nameof(CallbackText)); } set { Source.Set(nameof(CallbackText), value); } }
        public string? CallbackURL { get { return Source.Get<string?>(nameof(CallbackURL)); } set { Source.Set(nameof(CallbackURL), value); } }
        public bool CallbackAlert { get { return Source.Get<bool>(nameof(CallbackAlert)); } set { Source.Set(nameof(CallbackAlert), value); } }
    }
}