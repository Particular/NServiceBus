using NServiceBus.Logging;

namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Web;
    using Utils;

    public class HttpChannelSender : IChannelSender
    {
        public void Send(string remoteUrl,IDictionary<string,string> headers,Stream data)
        {
            var request = WebRequest.Create(remoteUrl);
            request.Method = "POST";


            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers = Encode(headers);
            request.UseDefaultCredentials = true;

            request.ContentLength = data.Length;
        
            using(var stream = request.GetRequestStream())
                data.CopyTo_net35(stream);

            int statusCode;

            //todo make the receiver send the md5 back so that we can double check that the transmission went ok
            using (var response = request.GetResponse() as HttpWebResponse)
                statusCode = (int)response.StatusCode;

            Logger.Debug("Got HTTP response with status code " + statusCode);


            if (statusCode != 200)
            {
                Logger.Warn("Message not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }
        }

        static WebHeaderCollection Encode(IDictionary<string,string> headers)
        {
            var webHeaders = new WebHeaderCollection();

            foreach (string header in headers.Keys)
                webHeaders.Add(HttpUtility.UrlEncode(header), HttpUtility.UrlEncode(headers[header]));

            return webHeaders;
        }

       
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}