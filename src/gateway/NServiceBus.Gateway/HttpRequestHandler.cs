using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NServiceBus.Unicast.Transport;
using System.IO;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Gateway
{
    public class HttpRequestHandler
    {
        public static void Handle(IContext ctx, MsmqTransport transport, string queue)
        {
            byte[] buffer = new byte[ctx.RequestContentLength];
            ctx.RequestInputStream.Read(buffer, 0, buffer.Length);

            string myHash = Hasher.Hash(buffer);
            string hash = ctx.RequestHeaders[Hasher.HeaderKey];

            if (myHash == hash)
            {
                TransportMessage msg = new TransportMessage();
                msg.BodyStream = new MemoryStream(buffer);

                HeaderMapper.Map(ctx.RequestHeaders, msg);

                transport.Send(msg, queue);
            }

            if (hash != null)
            {
                Console.WriteLine("Sending HTTP response.");

                byte[] b = System.Text.ASCIIEncoding.ASCII.GetBytes(hash);
                ctx.ResponseOutputStream.Write(b, 0, b.Length);
            }

            ctx.EndResponse();
        }
    }
}
