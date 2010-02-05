using System;
using System.Net;
using log4net;
using NServiceBus.Unicast.Transport;
using System.IO;

namespace NServiceBus.Gateway
{
    public class MsmqHandler
    {
        public static void Handle(TransportMessage msg, string remoteUrl)
        {
            Logger.Debug("Message received.");

            var request = WebRequest.Create(remoteUrl);
            request.Method = "POST";

            byte[] buffer = new byte[msg.BodyStream.Length];
            msg.BodyStream.Read(buffer, 0, (int)msg.BodyStream.Length);

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = buffer.Length;

            HeaderMapper.Map(msg, request.Headers);

            string hash = Hasher.Hash(buffer);
            request.Headers[Hasher.HeaderKey] = hash;

            request.GetRequestStream().Write(buffer, 0, buffer.Length);

            Logger.Debug("Sending HTTP Request.");

            var response = request.GetResponse();

            Logger.Debug("Got HTTP Response.");

            Stream s = response.GetResponseStream();
            byte[] b = new byte[1024];
            int read = s.Read(b, 0, 1024);
            byte[] bytes = new byte[read];

            for (int i = 0; i < read; i++)
                bytes[i] = b[i];

            string result = System.Text.Encoding.ASCII.GetString(bytes);

            if (result == hash)
                Logger.Debug("Message transferred successfully.");
            else
            {
                Logger.Info("Message not transferred successfully. Trying again...");
                throw new Exception();
            }
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
