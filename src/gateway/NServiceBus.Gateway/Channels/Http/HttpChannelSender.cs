namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Net;
    using log4net;
    using Unicast.Transport;

    public class HttpChannelSender:IChannelSender
    {
        public void Send(TransportMessage msg, string remoteUrl)
        {
            var headers = new WebHeaderCollection();

            if (!String.IsNullOrEmpty(msg.IdForCorrelation))
                msg.IdForCorrelation = msg.Id;

            HeaderMapper.Map(msg, headers);


            var buffer = msg.Body;


            var request = WebRequest.Create(remoteUrl);
            request.Method = "POST";


            request.ContentType = "application/x-www-form-urlencoded";


            string hash = Hasher.Hash(buffer);
            headers[HttpHeaders.ContentMd5Key] = hash;
            headers["NServiceBus.Gateway"] = "true";

            //todo the from key isn't used for anything, remove?
            headers[HttpHeaders.FromKey] = ListenUrl;
            headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof(CallType), CallType.Submit);

            request.Headers = headers;
            request.ContentLength = buffer.Length;

            var stream = request.GetRequestStream();
            stream.Write(buffer, 0, buffer.Length);

            Logger.Debug("Sending message to: " + remoteUrl);
            int statusCode;

            using (var response = request.GetResponse() as HttpWebResponse)
                statusCode = (int)response.StatusCode;

            Logger.Debug("Got HTTP response with status code " + statusCode);


            if (statusCode != 200)
            {
                Logger.Info("Message not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }

            Logger.Debug("Message transferred successfully. Going to acknowledge.");

            var ack = WebRequest.Create(remoteUrl);
            ack.Method = "POST";
            ack.ContentType = "application/x-www-form-urlencoded";
            ack.Headers = headers;
            ack.Headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof(CallType), CallType.Ack);
            ack.ContentLength = 0;

            Logger.Debug("Sending ack to: " + remoteUrl);

            int ackCode;
            using (var ackResponse = ack.GetResponse() as HttpWebResponse)
                ackCode = (int)ackResponse.StatusCode;

            Logger.Debug("Got HTTP response with status code " + ackCode);

            if (ackCode != 200)
            {
                Logger.Info("Ack not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }
        }

        public string ListenUrl { get; set; }


        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

    }
}