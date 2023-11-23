using PmEngine.Core;

namespace PmEngine.Telegram.Arguments
{
    public class InLineArguments : ActionArguments
    {
        public long InLineArgument { get { return Get<long>("inlineArgument"); } set { Set("inlineArgument", value); } }
        public int MessageActionId { get { return Get<int>("messageActionId"); } set { Set("messageActionId", value); } }

        public InLineArguments() { }

        public InLineArguments(long argument, int? messageActionId = null)
        {
            InLineArgument = argument;
            if (messageActionId.HasValue)
                MessageActionId = messageActionId.Value;
        }

        public InLineArguments(ActionArguments actionArguments)
        {
            Arguments = actionArguments.Arguments;
        }
    }
}