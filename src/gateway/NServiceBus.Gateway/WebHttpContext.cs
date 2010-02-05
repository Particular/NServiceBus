using System.Web;
using System.Collections.Specialized;
using System.IO;

namespace NServiceBus.Gateway
{
    public class WebHttpContext : IContext
    {
        private readonly HttpContext context;
        public WebHttpContext (HttpContext ctx)
        {
            context = ctx;
        }

        #region IContext Members

        public int RequestContentLength
        {
            get { return context.Request.ContentLength; }
        }

        public Stream RequestInputStream
        {
            get { return context.Request.InputStream; }
        }

        public Stream ResponseOutputStream
        {
            get { return context.Response.OutputStream; }
        }

        public NameValueCollection RequestHeaders
        {
            get { return context.Request.Headers; }
        }

        public void EndResponse()
        {
            context.Response.End();
        }

        #endregion
    }
}
