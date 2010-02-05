using System.Web;
using System.Net;

namespace NServiceBus.Gateway
{
    public static class Extensions
    {
        public static IContext AsIContext(this HttpContext context)
        {
            return new WebHttpContext(context);
        }

        public static IContext AsIContext(this HttpListenerContext context)
        {
            return new NetHttpContext(context);
        }
    }
}
