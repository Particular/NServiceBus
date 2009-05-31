using System;
using System.Net;
using System.Collections.Specialized;
using System.IO;

namespace NServiceBus.Gateway
{
    public class NetHttpContext : IContext
    {
        private HttpListenerContext context;
        public NetHttpContext(HttpListenerContext ctx)
        {
            context = ctx;
        }

        #region IContext Members

        public int RequestContentLength
        {
            get { return (int)context.Request.ContentLength64; }
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
            context.Response.OutputStream.Close();
        }

        #endregion
    }
}
