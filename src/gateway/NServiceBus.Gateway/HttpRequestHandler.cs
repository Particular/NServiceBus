using System;
using System.Net;
using log4net;
using NServiceBus.Unicast.Transport;
using System.IO;
using NServiceBus.Unicast.Transport.Msmq;
using System.Text;

namespace NServiceBus.Gateway
{
    internal class HttpRequestHandler
    {
        private const int maximumBytesToRead = 100000;
        private bool requireMD5FromClient = true;
        private readonly IMessageNotifier notifier;

        public HttpRequestHandler(bool requireMD5, IMessageNotifier notifier)
        {
            requireMD5FromClient = requireMD5;
            this.notifier = notifier;
        }

        public void Handle(HttpListenerContext ctx, MsmqTransport transport, string queue)
        {
            try
            {
                if (ctx.Request.ContentLength64 > 4*1024*1024)
                {
                    Logger.Warn("Received an Http request larger than 4MB, returning an error to the client " +
                                ctx.Request.RemoteEndPoint);
                    ctx.Response.StatusCode = 413; // Request Entity Too Large
                    ctx.Response.StatusDescription = "Cannot accept messages larger than 4MB.";

                    ctx.Response.Close(Encoding.ASCII.GetBytes("<html><body>" + ctx.Response.StatusDescription + "</body></html>"), false);
                    return;
                }

                string hash = ctx.Request.Headers[Headers.ContentMd5Key];
                if (hash == null && requireMD5FromClient)
                {
                    Logger.Warn("Received an Http request that didn't contain the " + Headers.ContentMd5Key +
                                " header from " + ctx.Request.RemoteEndPoint +
                                ". Rejecting the request. If you wish to allow these kinds of requests, please set the configuration field 'RequireMD5FromClient' to 'false'.");
                    ctx.Response.StatusCode = 400; //Bad Request
                    ctx.Response.StatusDescription = "Required header '" + Headers.ContentMd5Key +
                                                     "' missing. Cannot accept request.";

                    ctx.Response.Close(Encoding.ASCII.GetBytes("<html><body>" + ctx.Response.StatusDescription + "</body></html>"), false);

                    return;
                }

                var length = (int) ctx.Request.ContentLength64;
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

                string myHash = Hasher.Hash(buffer);
                TransportMessage msg;

                if (myHash == hash)
                {
                    msg = new TransportMessage
                              {
                                  BodyStream = new MemoryStream(buffer),
                                  ReturnAddress = transport.Address
                              };

                    //check to see if this is a gateway from another site
                    if (ctx.Request.Headers["NServiceBus.Gateway"] != null)
                        HeaderMapper.Map(ctx.Request.Headers, msg);
                    else
                    {
                        msg.MessageIntent = MessageIntentEnum.Send;
                        msg.Recoverable = true;
                        msg.Headers = new System.Collections.Generic.List<HeaderInfo>();
                    }

                    if (ctx.Request.Headers[Headers.FromKey] != null)
                        msg.Headers.Add(new HeaderInfo { Key = NServiceBus.Headers.HttpFrom, Value = ctx.Request.Headers[Headers.FromKey] });

                    var header = msg.Headers.Find(hi => hi.Key == HeaderMapper.RouteTo);
                    if (header != null)
                        transport.Send(msg, header.Value);
                    else
                        transport.Send(msg, queue);

                    notifier.RaiseMessageProcessed(TransportTypeEnum.FromHttpToMsmq, msg);
                }

                if (hash != null)
                {
                    Logger.Debug("Sending HTTP response.");

                    ctx.Response.AddHeader(Headers.ContentMd5Key, myHash);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.StatusDescription = "OK";

                    ctx.Response.Close(Encoding.ASCII.GetBytes("<html><body>" + ctx.Response.StatusDescription + "</body></html>"), false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error", ex);
                try
                {
                    ctx.Response.StatusCode = 502;
                    ctx.Response.StatusDescription = "Unexpected server error";
                    ctx.Response.Close(
                        Encoding.ASCII.GetBytes("<html><body>" + ctx.Response.StatusDescription + "</body></html>"),
                        false);
                }
                catch(Exception)
                {
                    Logger.Info("Could not notify client about exception.");
                }
            }

            Logger.Info("Http request processing complete.");
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
