namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using log4net;
    using System.Net;
    using Notifications;
    using Unicast.Transport;
    using System.Collections.Specialized;
    using System.Text;
    using System.Threading;
    using System.Transactions;
    using DataBus;
    using Persistence;

    public class HttpChannelReceiver:IChannelReceiver
    {
        public ChannelType Type
        {
            get { return ChannelType.Http; }
        }

       

        public void Start()
        {
            listener.Prefixes.Add(ListenUrl);

            ThreadPool.SetMaxThreads(NumberOfWorkerThreads, NumberOfWorkerThreads);

            listener.Start();

            new Thread(StartHttpServer).Start();

        }

        void StartHttpServer()
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

        public void Stop()
        {
           listener.Stop();
        }

        readonly ManualResetEvent hasStopped = new ManualResetEvent(false);
        public string ListenUrl { get; set; }

        public string ReturnAddress { get; set; }

        public IDataBus DataBus { get; set; }

        public int NumberOfWorkerThreads { get; set; }

        public event EventHandler<MessageForwardingArgs> MessageReceived;

        public HttpChannelReceiver(IPersistMessages persister)
        {
            this.persister = persister;

            listener = new HttpListener();
        }

        public void Handle(HttpListenerContext ctx)
        {
            try
            {
                var callInfo = GetCallInfo(ctx);
                
                if (callInfo.Type == CallType.Submit && ctx.Request.ContentLength64 > 4 * 1024 * 1024)
                {
                    CloseResponseAndWarn(ctx, "Cannot accept messages larger than 4MB.", 413);
                    return;
                }

                string hash = ctx.Request.Headers[HttpHeaders.ContentMd5Key];
                if (hash == null)
                {
                    CloseResponseAndWarn(ctx, "Required header '" + HttpHeaders.ContentMd5Key + "' missing.", 400);
                    return;
                }

                switch(callInfo.Type)
                {
                    case CallType.Submit: HandleSubmit(ctx, callInfo); break;
                    case CallType.DatabusProperty: HandleDatabusProperty(ctx, callInfo); break;
                    case CallType.Ack: HandleAck(ctx, callInfo); break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error", ex);
                CloseResponseAndWarn(ctx, "Unexpected server error", 502);
            }

            Logger.Info("Http request processing complete.");
        }

        void HandleDatabusProperty(HttpListenerContext ctx, CallInfo callInfo)
        {
            Logger.Debug("Received databus property, id: " + callInfo.ClientId);

            if(DataBus == null)
                throw new InvalidOperationException("Databus transmission received without a databus configured");


        }

        private void HandleAck(HttpListenerContext ctx, CallInfo callInfo)
        {
            Logger.Debug("Received message ack for id: " + callInfo.ClientId);

            var msg = new TransportMessage { ReturnAddress = ReturnAddress };

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30)}))
            {
                byte[] outMessage;
                NameValueCollection outHeaders;
                
                persister.AckMessage(callInfo.ClientId, Convert.FromBase64String(callInfo.MD5), out outMessage, out outHeaders);

                if (outHeaders != null && outMessage != null)
                {
                    msg.Body = outMessage;
                    
                    HeaderMapper.Map(outHeaders, msg);
                    if (msg.TimeToBeReceived < TimeSpan.FromSeconds(1))
                        msg.TimeToBeReceived = TimeSpan.FromSeconds(1);

                    msg.Recoverable = true;

                    if (String.IsNullOrEmpty(msg.IdForCorrelation))
                        msg.IdForCorrelation = msg.Id;

                    if (msg.MessageIntent == MessageIntentEnum.Init) // wasn't set by client
                        msg.MessageIntent = MessageIntentEnum.Send;


                    //todo the from key isn't used for anything, remove?
                    if (ctx.Request.Headers[HttpHeaders.FromKey] != null)
                        msg.Headers.Add(Headers.HttpFrom, ctx.Request.Headers[HttpHeaders.FromKey]);

                    
                    MessageReceived(this, new MessageForwardingArgs { Message = msg });
                }

                scope.Complete();
            }

            ReportSuccess(ctx);
        }

        private void HandleSubmit(HttpListenerContext ctx, CallInfo callInfo)
        {
            Logger.Debug("Received message submission for id: " + callInfo.ClientId);
            string hash = ctx.Request.Headers[HttpHeaders.ContentMd5Key];

            byte[] buffer = GetBuffer(ctx);
            string myHash = Hasher.Hash(buffer);

            if (myHash != hash)
            {
                CloseResponseAndWarn(ctx, "MD5 hash received does not match hash calculated on server. Consider resubmitting.", 412);
                return;
            }

            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) }))
            {
                persister.InsertMessage(DateTime.UtcNow, callInfo.ClientId, Convert.FromBase64String(callInfo.MD5), buffer, ctx.Request.Headers);

                scope.Complete();
            }

            ReportSuccess(ctx);
        }

        private void ReportSuccess(HttpListenerContext ctx)
        {
            Logger.Debug("Sending HTTP 200 response.");

            ctx.Response.StatusCode = 200;
            ctx.Response.StatusDescription = "OK";

            ctx.Response.Close(Encoding.ASCII.GetBytes(ctx.Response.StatusDescription), false);
        }

        private byte[] GetBuffer(HttpListenerContext ctx)
        {
            var length = (int)ctx.Request.ContentLength64;
            var buffer = new byte[length];

            int numBytesToRead = length;
            int numBytesRead = 0;
            while (numBytesToRead > 0)
            {
                int n = ctx.Request.InputStream.Read(
                    buffer, 
                    numBytesRead, 
                    numBytesToRead < maximumBytesToRead ? numBytesToRead : maximumBytesToRead);
                    
                if (n == 0)
                    break;

                numBytesRead += n;
                numBytesToRead -= n;
            }
            return buffer;
        }

        private CallInfo GetCallInfo(HttpListenerContext ctx)
        {
            var callTypeHeader = HeaderMapper.NServiceBus + HeaderMapper.CallType;
            string callType = ctx.Request.Headers[callTypeHeader];
            if (!Enum.IsDefined(typeof(CallType), callType))
            {
                CloseResponseAndWarn(ctx, "Required header '" + callTypeHeader + "' missing.", 400);
                return null;
            }

            var type = (CallType)Enum.Parse(typeof(CallType), callType);

            var clientIdHeader = HeaderMapper.NServiceBus + HeaderMapper.Id;
            var clientId = ctx.Request.Headers[clientIdHeader];
            if (clientId == null)
            {
                CloseResponseAndWarn(ctx, "Required header '" + clientIdHeader + "' missing.", 400);
                return null;
            }

            return new CallInfo
                       {
                           ClientId = ctx.Request.Headers[HeaderMapper.NServiceBus + HeaderMapper.Id],
                           MD5 = ctx.Request.Headers[HttpHeaders.ContentMd5Key],
                           Type = type
                       };
        }

        private static void CloseResponseAndWarn(HttpListenerContext ctx, string warning, int statusCode)
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
                Logger.Warn("Could not return warning to client.", e);
            }
        }

        readonly HttpListener listener;
        const int maximumBytesToRead = 100000;
        readonly IPersistMessages persister;
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

    }
}
