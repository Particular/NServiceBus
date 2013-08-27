namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using System.Web;
    using HeaderManagement;
    using Logging;
    using Utils;

    public class HttpChannelReceiver : IChannelReceiver
    {
        public event EventHandler<DataReceivedOnChannelArgs> DataReceived;

        private MTATaskScheduler scheduler;
        private bool disposed;
        CancellationTokenSource tokenSource;

        public void Start(string address, int numWorkerThreads)
        {
            tokenSource = new CancellationTokenSource();
            listener = new HttpListener();
        
            listener.Prefixes.Add(address);

            scheduler = new MTATaskScheduler(numWorkerThreads, String.Format("NServiceBus Gateway Channel Receiver Thread for [{0}]", address));

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to start listener for {0} make sure that you have admin privileges", address), ex);
            }

            var token = tokenSource.Token;
            Task.Factory.StartNew(HttpServer, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                tokenSource.Cancel();

                listener.Stop();
                
                scheduler.Dispose();
            }

            disposed = true;
        }

        ~HttpChannelReceiver()
        {
            Dispose(false);
        }
   
        public void Handle(HttpListenerContext ctx)
        {
            try
            {
                if(!IsGatewayRequest(ctx.Request))
                {
                    //there will always be a responder
                    Configure.Instance.Builder.Build<IHttpResponder>().Handle(ctx);
                    return;
                }

                DataReceived(this, new DataReceivedOnChannelArgs
                                       {
                                           Headers = GetHeaders(ctx),
                                           Data = GetMessageStream(ctx)
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

        static MemoryStream GetMessageStream(HttpListenerContext ctx)
        {
            if(ctx.Request.QueryString.AllKeys.Contains("Message"))
            {
                var message = HttpUtility.UrlDecode(ctx.Request.QueryString["Message"]);

                return new MemoryStream(Encoding.UTF8.GetBytes(message));
            }
                
            var streamToReturn = new MemoryStream();

            ctx.Request.InputStream.CopyTo_net35(streamToReturn, MaximumBytesToRead);
            streamToReturn.Position = 0;
            return streamToReturn;
        }

        bool IsGatewayRequest(HttpListenerRequest request)
        {
            return request.Headers.AllKeys.Contains(GatewayHeaders.CallTypeHeader) ||
                   request.Headers.AllKeys.Contains(GatewayHeaders.CallTypeHeader.ToLower()) ||
                   request.QueryString[GatewayHeaders.CallTypeHeader] != null;
        }


        static IDictionary<string,string> GetHeaders(HttpListenerContext ctx)
        {
            var headers = new Dictionary<string,string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (string header in ctx.Request.Headers.Keys)
                headers.Add(HttpUtility.UrlDecode(header), HttpUtility.UrlDecode(ctx.Request.Headers[header]));
            
            foreach (string header in ctx.Request.QueryString.Keys)
                headers[HttpUtility.UrlDecode(header)] = HttpUtility.UrlDecode(ctx.Request.QueryString[header]);

            return headers;
        }


        void HttpServer(object o)
        {
            var cancellationToken = (CancellationToken)o;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var ctx = listener.GetContext();
                    new Task(() => Handle(ctx)).Start(scheduler);
                }
                catch (HttpListenerException ex)
                {
                    // a HttpListenerException can occur on listener.GetContext when we shutdown. this can be ignored
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Logger.Error("Gateway failed to receive incoming request.", ex);
                    }
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error("Gateway failed to receive incoming request.", ex);
                    break;
                }
            }
        }

        static void ReportSuccess(HttpListenerContext ctx)
        {
            Logger.Debug("Sending HTTP 200 response.");

            ctx.Response.StatusCode = 200;
            ctx.Response.StatusDescription = "OK";

          
            WriteData(ctx,"OK");
        }

        static void WriteData(HttpListenerContext ctx,string status)
        {
            var str = status;

            var jsonp = ctx.Request.QueryString["callback"];
            if (string.IsNullOrEmpty(jsonp) == false)
            {
                str = jsonp + "({ status: '" + str + "'})";
                ctx.Response.AddHeader("Content-Type", "application/javascript; charset=utf-8");
            }
            else
            {
                ctx.Response.AddHeader("Content-Type", "application/json; charset=utf-8");
            }
            ctx.Response.Close(Encoding.ASCII.GetBytes(str), false);
        }
        
        static void CloseResponseAndWarn(HttpListenerContext ctx, string warning, int statusCode)
        {
            try
            {
                Logger.WarnFormat("Cannot process HTTP request from {0}. Reason: {1}.", ctx.Request.RemoteEndPoint, warning);
                ctx.Response.StatusCode = statusCode;
                ctx.Response.StatusDescription = warning;

                WriteData(ctx, warning);
            }
            catch (Exception e)
            {
                Logger.Error("Could not return warning to client.", e);
            }
        }

        HttpListener listener;
        
        const int MaximumBytesToRead = 100000;
        
        static readonly ILog Logger = LogManager.GetLogger(typeof(HttpChannelReceiver));
    }
}
