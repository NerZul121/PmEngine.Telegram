using PmEngine.Core;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Arguments;

namespace PmEngine.Telegram.Extensions
{
    public static class ArgsExt
    {
        public static int InputMessageId(this IActionArguments actionArguments)
        {
            return actionArguments.Get<int>("inputMessageId");
        }

        public static void InputMessageId(this IActionArguments actionArguments, int msgId)
        {
            actionArguments.Set("inputMessageId", msgId);
        }
        public static long InLineArgument(this IActionArguments actionArguments)
        {
            return actionArguments.Get<long>("inlineArgument");
        }
        public static void InLineArgument(this IActionArguments actionArguments, int msgId)
        {
            actionArguments.Set("inlineArgument", msgId);
        }

        public static InLineArguments ToInLineArguments(this ActionArguments actionArguments)
        {
            return new InLineArguments(actionArguments);
        }
    }
}