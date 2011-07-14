namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web;
    using log4net;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Utils;

    public class HttpChannelReceiver:IChannelReceiver
    {
        public event EventHandler<DataReceivedOnChannelArgs> DataReceived;

       
        public void Start(string address, int numWorkerThreads)
        {
            listener = new HttpListener();
        
            listener.Prefixes.Add(address);

            ThreadPool.SetMaxThreads(numWorkerThreads, numWorkerThreads);

            listener.Start();

            new Thread(HttpServer).Start();
        }

        public void Dispose()
        {
            listener.Stop();
        }

        public void Handle(HttpListenerContext ctx)
        {
            try
            {
                var streamToReturn = new MemoryStream();

                ctx.Request.InputStream.CopyTo_net35(streamToReturn, MaximumBytesToRead);
                streamToReturn.Position = 0;
                
                DataReceived(this, new DataReceivedOnChannelArgs
                                       {
                                           Headers = GetHeaders(ctx),
                                           Data = streamToReturn
                                       });
                ReportSuccess(ctx);

                Logger.Debug("Http request processing complete.");
            }
            catch (HttpChannelException ex)
            {
                CloseResponseAndWarn(ctx, ex.Message, ex.StatusCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error", ex);
                CloseResponseAndWarn(ctx, "Unexpected server error", 502);
            }
        }


        static IDictionary<string,string> GetHeaders(HttpListenerContext ctx)
        {
            var headers = new Dictionary<string,string>();

            foreach (string header in ctx.Request.Headers.Keys)
                headers.Add(HttpUtility.UrlDecode(header), HttpUtility.UrlDecode(ctx.Request.Headers[header]));

            return headers;
        }


        void HttpServer()
        {
            while (true)
            {
                try
                {
                    var ctx = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(x => Handle(ctx));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }

            hasStopped.Set();
        }

        static void ReportSuccess(HttpListenerContext ctx)
        {
            Logger.Debug("Sending HTTP 200 response.");

            ctx.Response.StatusCode = 200;
            ctx.Response.StatusDescription = "OK";

            ctx.Response.Close(Encoding.ASCII.GetBytes(ctx.Response.StatusDescription), false);
        }

        
        static void CloseResponseAndWarn(HttpListenerContext ctx, string warning, int statusCode)
        {
            try
            {
                Logger.WarnFormat("Cannot process HTTP request from {0}. Reason: {1}.", ctx.Request.RemoteEndPoint, warning);
                ctx.Response.StatusCode = statusCode;
                ctx.Response.StatusDescription = warning;

                ctx.Response.Close(Encoding.ASCII.GetBytes(warning), false);
            }
            catch (Exception e)
            {
                Logger.Error("Could not return warning to client.", e);
            }
        }

        readonly ManualResetEvent hasStopped = new ManualResetEvent(false);
        
        HttpListener listener;
        
        const int MaximumBytesToRead = 100000;
        
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
