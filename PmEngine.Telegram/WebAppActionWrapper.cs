using PmEngine.Core;
using PmEngine.Telegram.Interfaces;

namespace PmEngine.Telegram
{
    public class WebAppActionWrapper : ActionWrapper, IWebAppActionWrapper
    {
        public string Url { get; set; }

        public WebAppActionWrapper(string name, string url = "", Type? actionClass = null) : base(name, actionClass)
        {
            Url = url;
        }
    }
}