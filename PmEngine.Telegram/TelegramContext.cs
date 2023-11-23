using PmEngine.Core.BaseClasses;
using PmEngine.Core.Interfaces;

namespace PmEngine.Telegram
{
    public class TelegramContext : BaseContext
    {
        public TelegramContext(IEngineConfigurator configurator = null) : base(configurator)
        {
        }
    }
}