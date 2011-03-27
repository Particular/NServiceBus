using System;
using log4net;
using System.Net;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Gateway
{
    public class HttpSender
    {
        private readonly string from;
        private readonly IMessageNotifier notifier;

        public HttpSender(IMessageNotifier notifier, string listenUrl)
        {
            this.notifier = notifier;
            from = listenUrl;
        }
        public void Send(TransportMessage msg, string remoteUrl)
        {
            var address = remoteUrl;
            var headers = new WebHeaderCollection();

            if (msg.Headers.ContainsKey(NServiceBus.Headers.HttpTo))
                address = msg.Headers[NServiceBus.Headers.HttpTo];

            var request = WebRequest.Create(address);
            request.Method = "POST";

            var buffer = msg.Body;

            request.ContentType = "application/x-www-form-urlencoded";

            if (!String.IsNullOrEmpty(msg.IdForCorrelation))
                msg.IdForCorrelation = msg.Id;

            HeaderMapper.Map(msg, headers);

            string hash = Hasher.Hash(buffer);
            headers[HttpHeaders.ContentMd5Key] = hash;
            headers["NServiceBus.Gateway"] = "true";
            headers[HttpHeaders.FromKey] = from;
            headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof (CallType), CallType.Submit);

            request.Headers = headers;
            request.ContentLength = buffer.Length;

            var stream = request.GetRequestStream();
            stream.Write(buffer, 0, buffer.Length);

            Logger.Debug("Sending message to: " + address);

            var response = request.GetResponse() as HttpWebResponse;
            var statusCode = (int)response.StatusCode;
            response.Close();

            Logger.Debug("Got HTTP response with status code " + statusCode);


            if (statusCode == 200)
            {
                Logger.Debug("Message transferred successfully. Going to acknowledge.");

                var ack = WebRequest.Create(address);
                ack.Method = "POST";
                ack.ContentType = "application/x-www-form-urlencoded";
                ack.Headers = headers;
                ack.Headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof(CallType), CallType.Ack);
                ack.ContentLength = 0;
                
                Logger.Debug("Sending ack to: " + address);

                var ackResponse = ack.GetResponse() as HttpWebResponse;
                var ackCode = (int)ackResponse.StatusCode;
                response.Close();

                Logger.Debug("Got HTTP response with status code " + ackCode);

                if (ackCode != 200)
                {
                    Logger.Info("Ack not transferred successfully. Trying again...");
                    throw new Exception("Retrying");
                }

                notifier.RaiseMessageProcessed(TransportTypeEnum.FromMsmqToHttp, msg);
            }
            else
            {
                Logger.Info("Message not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
