using PmEngine.Core;
using PmEngine.Telegram.Interfaces;

namespace PmEngine.Telegram
{
    public class UrlActionWrapper : ActionWrapper, IWebAppActionWrapper
    {
        public string Url { get; set; }

        public UrlActionWrapper(string name, string url = "", Type? actionClass = null) : base(name, actionClass)
        {
            Url = url;
        }
    }
}