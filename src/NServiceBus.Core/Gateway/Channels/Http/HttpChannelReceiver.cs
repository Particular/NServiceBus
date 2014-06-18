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
    using System.Web;
    using HeaderManagement;
    using Logging;
    using Receiving;

    public class HttpChannelReceiver : IChannelReceiver
    {
        public event EventHandler<DataReceivedOnChannelArgs> DataReceived;

        public void Start(string address, int numberOfWorkerThreads)
        {
            tokenSource = new CancellationTokenSource();
            listener = new HttpListener();

            listener.Prefixes.Add(address);

            scheduler = new MTATaskScheduler(numberOfWorkerThreads, String.Format("NServiceBus Gateway Channel Receiver Thread for [{0}]", address));

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                var message = string.Format("Failed to start listener for {0} make sure that you have admin privileges", address);
                throw new Exception(message,ex);
            }

            var token = tokenSource.Token;
            Task.Factory.StartNew(HttpServer, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
            if (listener != null)
            {
                listener.Close();
            }
            if (scheduler != null)
            {
                scheduler.Dispose();
            }
        }

        public void Handle(HttpListenerContext context)
        {
            try
            {
                if (!IsGatewayRequest(context.Request))
                {
                    //there will always be a responder
                    Configure.Instance.Builder.Build<IHttpResponder>().Handle(context);
                    return;
                }

                DataReceived(this, new DataReceivedOnChannelArgs
                {
                    Headers = GetHeaders(context),
                    Data = GetMessageStream(context)
                });
                ReportSuccess(context);

                Logger.Debug("Http request processing complete.");
            }
            catch (ChannelException ex)
            {
                CloseResponseAndWarn(context, ex.GetMessage(), ex.StatusCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error", ex);
                CloseResponseAndWarn(context, "Unexpected server error", 502);
            }
        }

        static MemoryStream GetMessageStream(HttpListenerContext context)
        {
            if (context.Request.QueryString.AllKeys.Contains("Message"))
            {
                var message = HttpUtility.UrlDecode(context.Request.QueryString["Message"]);

                return new MemoryStream(Encoding.UTF8.GetBytes(message));
            }

            var streamToReturn = new MemoryStream();

            context.Request.InputStream.CopyTo(streamToReturn, MaximumBytesToRead);
            streamToReturn.Position = 0;

            return streamToReturn;
        }

        bool IsGatewayRequest(HttpListenerRequest request)
        {
            return request.Headers.AllKeys.Contains(GatewayHeaders.CallTypeHeader) ||
                   request.Headers.AllKeys.Contains(GatewayHeaders.CallTypeHeader.ToLower()) ||
                   request.QueryString[GatewayHeaders.CallTypeHeader] != null;
        }


        static IDictionary<string, string> GetHeaders(HttpListenerContext context)
        {
            var headers = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (string header in context.Request.Headers.Keys)
            {
                headers.Add(HttpUtility.UrlDecode(header), HttpUtility.UrlDecode(context.Request.Headers[header]));
            }

            foreach (string header in context.Request.QueryString.Keys)
            {
                headers[HttpUtility.UrlDecode(header)] = HttpUtility.UrlDecode(context.Request.QueryString[header]);
            }

            return headers;
        }


        void HttpServer(object o)
        {
            var cancellationToken = (CancellationToken) o;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = listener.GetContext();
                    new Task(() => Handle(context)).Start(scheduler);
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

        static void ReportSuccess(HttpListenerContext context)
        {
            Logger.Debug("Sending HTTP 200 response.");

            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";

            WriteData(context, "OK");
        }

        static void WriteData(HttpListenerContext context, string status)
        {
            var newStatus = status;

            var jsonCallback = context.Request.QueryString["callback"];
            if (string.IsNullOrEmpty(jsonCallback) == false)
            {
                newStatus = jsonCallback + "({ status: '" + newStatus + "'})";
                context.Response.AddHeader("Content-Type", "application/javascript; charset=utf-8");
            }
            else
            {
                context.Response.AddHeader("Content-Type", "application/json; charset=utf-8");
            }
            context.Response.Close(Encoding.ASCII.GetBytes(newStatus), false);
        }

        static void CloseResponseAndWarn(HttpListenerContext context, string warning, int statusCode)
        {
            try
            {
                Logger.WarnFormat("Cannot process HTTP request from {0}. Reason: {1}.", context.Request.RemoteEndPoint, warning);
                context.Response.StatusCode = statusCode;
                context.Response.StatusDescription = warning;

                WriteData(context, warning);
            }
            catch (Exception e)
            {
                Logger.Error("Could not return warning to client.", e);
            }
        }

        const int MaximumBytesToRead = 100000;

        static ILog Logger = LogManager.GetLogger<HttpChannelReceiver>();
        HttpListener listener;
        MTATaskScheduler scheduler;
        CancellationTokenSource tokenSource;
    }
}
