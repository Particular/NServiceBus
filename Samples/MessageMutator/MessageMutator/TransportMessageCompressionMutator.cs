using System.IO;
using System.IO.Compression;
using System.Linq;
using Messages;
using NServiceBus.MessageMutator;
using NServiceBus.Unicast.Transport;

namespace MessageMutators
{
    public class TransportMessageCompressionMutator : IMutateTransportMessages
    {

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            // Do the compression on MessageWithByteArray[] only
            if (messages.Any(message => message as MessageWithByteArray == null))
                return;
            
            var mStream = new MemoryStream(transportMessage.Body);
            var outStream = new MemoryStream();

            using (var tinyStream = new GZipStream(outStream, CompressionMode.Compress))
            {
                mStream.CopyTo(tinyStream);
            }
            // copy the compressed buffer only after the GZipStream is disposed, 
            // otherwise, not all the compressed message will be copied.
            transportMessage.Body = outStream.ToArray();
            transportMessage.Headers["IWasCompressed"] = "true";
        }

        public void MutateIncoming(TransportMessage transportMessage)
        {
            if (!transportMessage.Headers.ContainsKey("IWasCompressed"))
                return;

            using (var bigStream = new GZipStream(new MemoryStream(transportMessage.Body), CompressionMode.Decompress))
            {
                var bigStreamOut = new MemoryStream();
                bigStream.CopyTo(bigStreamOut);
                transportMessage.Body = bigStreamOut.ToArray();
            }
        }
    }
}
