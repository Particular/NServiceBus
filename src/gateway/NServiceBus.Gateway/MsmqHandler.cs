using System;
using NServiceBus.Unicast.Transport.Msmq;
using System.Net;
using NServiceBus.Unicast.Transport;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace NServiceBus.Gateway
{
    public class MsmqHandler
    {
        public static void Handle(TransportMessage msg, string remoteUrl)
        {
            Console.WriteLine("Message received.");

            var request = WebRequest.Create(remoteUrl);
            request.Method = "POST";

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = msg.Body.Length;

            HeaderMapper.Map(msg, request.Headers);

            string hash = Hasher.Hash(msg.Body);
            request.Headers[Hasher.HeaderKey] = hash;

            request.GetRequestStream().Write(msg.Body, 0, msg.Body.Length);

            Console.WriteLine("Sending HTTP Request.");

            var response = request.GetResponse();

            Console.WriteLine("Got HTTP Response.");

            Stream s = response.GetResponseStream();
            byte[] b = new byte[1024];
            int read = s.Read(b, 0, 1024);
            byte[] bytes = new byte[read];

            for (int i = 0; i < read; i++)
                bytes[i] = b[i];

            string result = Encoding.ASCII.GetString(bytes);

            if (result == hash)
                Console.WriteLine("Message transferred successfully.");
            else
            {
                Console.WriteLine("Message not transferred successfully. Trying again...");
                throw new Exception();
            }
        }
    }
}
