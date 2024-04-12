namespace PmEngine.Telegram.Arguments
{
    public class InLineArguments : Core.Arguments
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

        public InLineArguments(Core.Arguments actionArguments)
        {
            Source = actionArguments.Source;
        }
    }
}