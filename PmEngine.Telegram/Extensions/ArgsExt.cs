using PmEngine.Telegram.Arguments;
using Telegram.Bot.Types;

namespace PmEngine.Telegram.Extensions
{
    public static class ArgsExt
    {
        public static int InputMessageId(this Core.Arguments actionArguments)
        {
            return actionArguments.Get<int>("inputMessageId");
        }

        public static void InputMessageId(this Core.Arguments actionArguments, int msgId)
        {
            actionArguments.Set("inputMessageId", msgId);
        }
        public static long InLineArgument(this Core.Arguments actionArguments)
        {
            return actionArguments.Get<long>("inlineArgument");
        }
        public static void InLineArgument(this Core.Arguments actionArguments, int msgId)
        {
            actionArguments.Set("inlineArgument", msgId);
        }

        public static CallbackQuery? CallbackQuery(this Core.Arguments actionArguments)
        {
            return actionArguments.Get<CallbackQuery?>("CallbackQuery");
        }

        public static void CallbackQuery(this Core.Arguments actionArguments, CallbackQuery callbackQuery)
        {
            actionArguments.Set("CallbackQuery", callbackQuery);
        }

        public static InLineArguments ToInLineArguments(this Core.Arguments actionArguments)
        {
            return new InLineArguments(actionArguments);
        }
    }
}