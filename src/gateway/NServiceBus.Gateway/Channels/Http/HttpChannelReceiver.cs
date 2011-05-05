﻿namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web;
    using HeaderManagement;
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
        public HttpChannelReceiver(IPersistMessages persister)
        {
            this.persister = persister;

            listener = new HttpListener();
        }


        public IDataBus DataBus { get; set; }

        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

       
        public void Start(string address, int numWorkerThreads)
        {
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
                var callInfo = GetCallInfo(ctx);

                Logger.DebugFormat("Received message of type {0} for client id: {1}",callInfo.Type, callInfo.ClientId);

     
                //todo this is a msmq specific validation and should be moved to the layer above that is sending the message onto the main transport
                if (callInfo.Type == CallType.Submit && ctx.Request.ContentLength64 > 4 * 1024 * 1024)
                    throw new HttpChannelException(413, "Cannot accept messages larger than 4MB.");

               
                switch(callInfo.Type)
                {
                    case CallType.Submit: HandleSubmit(callInfo); break;
                    case CallType.DatabusProperty: HandleDatabusProperty(callInfo); break;
                    case CallType.Ack: HandleAck(callInfo); break;
                }

                ReportSuccess(ctx);
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

            Logger.Info("Http request processing complete.");
        }

        void HandleSubmit(CallInfo callInfo)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) }))
            {
                persister.InsertMessage(callInfo.ClientId,DateTime.UtcNow, callInfo.Buffer, callInfo.Headers);

                scope.Complete();
            }
        }
        
        void HandleDatabusProperty(CallInfo callInfo)
        {
            if(DataBus == null)
                throw new InvalidOperationException("Databus transmission received without a databus configured");

            TimeSpan timeToBeReceived;

            if (!TimeSpan.TryParse(callInfo.Headers["NServiceBus.TimeToBeReceived"], out timeToBeReceived))
                timeToBeReceived = TimeSpan.FromHours(1);
           
            string newDatabusKey;

            using(var stream = new MemoryStream(callInfo.Buffer))
                newDatabusKey = DataBus.Put(stream,timeToBeReceived);
     
            
           
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) }))
            {
                persister.UpdateHeader(callInfo.ClientId, callInfo.Headers[GatewayHeaders.DatabusKey], newDatabusKey);

                scope.Complete();
            }
        }

        void HandleAck(CallInfo callInfo)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30)}))
            {
                byte[] outMessage;
                NameValueCollection outHeaders;
                
                persister.AckMessage(callInfo.ClientId, out outMessage, out outHeaders);

                if (outHeaders != null && outMessage != null)
                {
                    var msg = new TransportMessage
                                  {
                                      Body = outMessage,
                                      Headers = new Dictionary<string, string>(),
                                      MessageIntent = MessageIntentEnum.Send,
                                      Recoverable = true
                                  };


                    if (outHeaders[GatewayHeaders.IsGatewayMessage] != null)
                        HeaderMapper.Map(outHeaders, msg);
                   
                  
                    MessageReceived(this, new MessageReceivedOnChannelArgs { Message = msg });
                }

                scope.Complete();
            }
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

        void ReportSuccess(HttpListenerContext ctx)
        {
            Logger.Debug("Sending HTTP 200 response.");

            ctx.Response.StatusCode = 200;
            ctx.Response.StatusDescription = "OK";

            ctx.Response.Close(Encoding.ASCII.GetBytes(ctx.Response.StatusDescription), false);
        }

        byte[] GetBuffer(HttpListenerContext ctx)
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

        CallInfo GetCallInfo(HttpListenerContext ctx)
        {
            var headers = GetHeaders(ctx);


            string callType = headers[CallTypeHeader];
            if (!Enum.IsDefined(typeof(CallType), callType))
                throw new HttpChannelException(400, "Required header '" + CallTypeHeader + "' missing.");

            var type = (CallType)Enum.Parse(typeof(CallType), callType);

            var clientId = headers[ClientIdHeader];
            if (clientId == null)
                throw new HttpChannelException(400, "Required header '" + ClientIdHeader + "' missing.");

            var md5 = headers[HttpHeaders.ContentMd5Key];

            if (md5== null)
                throw new HttpChannelException(400, "Required header '" + HttpHeaders.ContentMd5Key + "' missing.");

            byte[] buffer = GetBuffer(ctx);

            if (buffer.Length > 0 && Hasher.Hash(buffer) != md5)
                throw new HttpChannelException(412, "MD5 hash received does not match hash calculated on server. Consider resubmitting.");

              
            return new CallInfo
                       {
                           ClientId = clientId,
                           Type = type,
                           Headers = headers,
                           Buffer = buffer
                       };
        }

        static NameValueCollection GetHeaders(HttpListenerContext ctx)
        {
            var headers = new NameValueCollection();

            foreach (string header in ctx.Request.Headers.Keys)
                headers.Add(HttpUtility.UrlDecode(header), HttpUtility.UrlDecode(ctx.Request.Headers[header]));

            return headers;
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
                Logger.Warn("Could not return warning to client.", e);
            }
        }

        readonly ManualResetEvent hasStopped = new ManualResetEvent(false);
      
        const string CallTypeHeader = HeaderMapper.NServiceBus + HeaderMapper.CallType;
        
        const string ClientIdHeader = HeaderMapper.NServiceBus + HeaderMapper.Id;
        
        readonly HttpListener listener;
        
        const int maximumBytesToRead = 100000;
        
        readonly IPersistMessages persister;
        
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

    }
}
