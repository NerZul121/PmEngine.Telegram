using Microsoft.Extensions.DependencyInjection;
using PmEngine.Core;
using PmEngine.Core.Interfaces;
using PmEngine.Telegram.Interfaces;
using PmEngine.Telegram.Models;

namespace PmEngine.Telegram.Extensions
{
    public static class InLineWrapperExt
    {
        public static string ToInLineModel(this ActionWrapper wrapper)
        {
            var model = new InlineWrapperModel();
            model.ActionTypeName = wrapper.ActionType?.ToString();
            model.MessageActionId = wrapper.Arguments.Get<int?>("messageActionId") ?? (string.IsNullOrEmpty(model.ActionTypeName) ? 1 : -1);
            model.Argument = wrapper.Arguments.Get<long?>("inlineArgument") ?? 0;

            var stred = $"{wrapper.ActionType}:{model.MessageActionId}:{model.Argument}";
            if (stred.Length > 64)
                Console.WriteLine("WARN: InLine DATA > 64 s: " + stred);

            return stred;
        }

        public static InlineWrapperModel? GetInLineModel(this string stringModel)
        {
            var args = stringModel.Split(':');
            return new InlineWrapperModel() { ActionTypeName = args[0], MessageActionId = int.Parse(args[1]), Argument = long.Parse(args[2]) };
        }

        public static ActionWrapper? ToWrapper(this InlineWrapperModel? model, IServiceProvider services)
        {
            if (model is null)
                return null;

            var at = services.GetServices<IAction>().FirstOrDefault(a => a.GetType().ToString() == model.ActionTypeName)?.GetType();
            var args = new Core.Arguments();

            if (model.MessageActionId == -1)
                model.MessageActionId = (int)services.GetRequiredService<ITelegramOutputConfigure>().DefaultInLineMessageAction;

            args.Set("messageactionid", model.MessageActionId);
            args.Set("inlineArgument", model.Argument);
            var wrapepr = new ActionWrapper("", at, args);

            return wrapepr;
        }
    }
}