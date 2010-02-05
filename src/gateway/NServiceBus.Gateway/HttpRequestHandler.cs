using log4net;
using NServiceBus.Unicast.Transport;
using System.IO;
using NServiceBus.Unicast.Transport.Msmq;

namespace NServiceBus.Gateway
{
    public class HttpRequestHandler
    {
        public static void Handle(IContext ctx, MsmqTransport transport, string queue)
        {
            var buffer = new byte[ctx.RequestContentLength];
            ctx.RequestInputStream.Read(buffer, 0, buffer.Length);

            string myHash = Hasher.Hash(buffer);
            string hash = ctx.RequestHeaders[Hasher.HeaderKey];

            if (myHash == hash)
            {
                var msg = new TransportMessage {BodyStream = new MemoryStream(buffer)};

                HeaderMapper.Map(ctx.RequestHeaders, msg);

                transport.Send(msg, queue);
            }

            if (hash != null)
            {
                Logger.Debug("Sending HTTP response.");

                byte[] b = System.Text.Encoding.ASCII.GetBytes(hash);
                ctx.ResponseOutputStream.Write(b, 0, b.Length);
            }

            ctx.EndResponse();
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
