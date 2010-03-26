using System;
using log4net;
using System.Net;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Gateway
{
    public class MsmqHandler
    {
        private readonly string from;
        public MsmqHandler(string listenUrl)
        {
            from = listenUrl;
        }
        public void Handle(TransportMessage msg, string remoteUrl)
        {
            var address = remoteUrl;

            var header = msg.Headers.Find(h => h.Key == NServiceBus.Headers.HttpTo);
            if (header != null)
                address = header.Value;

            var request = WebRequest.Create(address);
            request.Method = "POST";

            var buffer = msg.Body;

            request.ContentType = "application/x-www-form-urlencoded";

            HeaderMapper.Map(msg, request.Headers);

            string hash = Hasher.Hash(buffer);
            request.Headers[Headers.ContentMd5Key] = hash;
            request.Headers["NServiceBus.Gateway"] = "true";
            request.Headers[Headers.FromKey] = from;

            request.ContentLength = buffer.Length;

            var stream = request.GetRequestStream();
            stream.Write(buffer, 0, buffer.Length);

            Logger.Debug("Sending message to: " + address);

            var response = request.GetResponse();

            Logger.Debug("Got HTTP Response, going to check MD5.");

            var md5 = response.Headers[Headers.ContentMd5Key];
            response.Close();

            if (md5 == null)
            {
                Logger.Error("Integration Error: Response did not contain necessary header " + Headers.ContentMd5Key + ". Can't be sure that data arrived intact at target " + address);
                return;
            }

            if (md5 == hash)
                Logger.Debug("Message transferred successfully.");
            else
            {
                Logger.Info(Headers.ContentMd5Key + " header received from client not the same as that sent. Message not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
