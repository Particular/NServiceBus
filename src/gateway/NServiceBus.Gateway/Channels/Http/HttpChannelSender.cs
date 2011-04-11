namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Specialized;
    using System.Net;
    using System.Web;
    using DataBus;
    using log4net;

    public class HttpChannelSender : IChannelSender
    {
        public ChannelType Type { get { return ChannelType.Http; } }


        public void Send(string remoteUrl,NameValueCollection headers,byte[] body)
        {
            MakeHttpRequest(remoteUrl, CallType.Submit, headers, body);

            TransmittDataBusProperties(remoteUrl, headers);

            MakeHttpRequest(remoteUrl, CallType.Ack, headers, new byte[0]);
        }


        void TransmittDataBusProperties(string remoteUrl, NameValueCollection headers)
        {
            var headersToSend = new NameValueCollection {headers};


            foreach (string headerKey in headers.Keys)
            {
                if (headerKey.Contains(DATABUS_PREFIX))
                {
                    if (DataBus == null)
                        throw new InvalidOperationException("Can't send a message with a databus property without a databus configured");

                    headersToSend[GatewayHeaders.DatabusKey] = headerKey;

                    using (var stream = DataBus.Get(headers[headerKey]))
                    {
                        var buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, (int)stream.Length);

                        MakeHttpRequest(remoteUrl, CallType.DatabusProperty, headersToSend, buffer);
                    }
                }

            }

        }

        void MakeHttpRequest(string remoteUrl, CallType callType, NameValueCollection headers, byte[] buffer)
        {
            headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof(CallType), callType);
            headers[HttpHeaders.ContentMd5Key] = Hasher.Hash(buffer);
            headers["NServiceBus.Gateway"] = "true";

            headers[HttpHeaders.FromKey] = ListenUrl;
  

            var request = WebRequest.Create(remoteUrl);
            request.Method = "POST";


            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers = Encode(headers);


            request.ContentLength = buffer.Length;

            var stream = request.GetRequestStream();
            stream.Write(buffer, 0, buffer.Length);

            Logger.DebugFormat("Sending message - {0} to: {1}", callType, remoteUrl);
            int statusCode;

            using (var response = request.GetResponse() as HttpWebResponse)
                statusCode = (int)response.StatusCode;

            Logger.Debug("Got HTTP response with status code " + statusCode);


            if (statusCode != 200)
            {
                Logger.Warn("Message not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }
        }

        static WebHeaderCollection Encode(NameValueCollection headers)
        {
            var webHeaders = new WebHeaderCollection();

            foreach (string header in headers.Keys)
                webHeaders.Add(HttpUtility.UrlEncode(header), HttpUtility.UrlEncode(headers[header]));

            return webHeaders;
        }

        public IDataBus DataBus { get; set; }

        public string ListenUrl { get; set; }

        const string DATABUS_PREFIX = "NServiceBus.DataBus.";

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}