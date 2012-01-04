using System.IO;
using System.IO.Compression;
using Messages;
using NServiceBus.MessageMutator;

namespace MessageMutators
{
    public class CompressionMutatorForASingleProperty : IMessageMutator
    {
        public object MutateOutgoing(object message)
        {
            var msg = message as MessageWithDoubleAndByteArray;
            if (msg == null)
                return message;

            var mStream = new MemoryStream(msg.Buffer);
            var outStream = new MemoryStream();
            
            using (var tinyStream = new GZipStream(outStream, CompressionMode.Compress))
            {
                mStream.CopyTo(tinyStream);
            }
            // copy the compressed buffer only after the GZipStream is disposed, otherwise, not all the compressed message will be copied.
            msg.Buffer = outStream.ToArray();
            return msg;
        }

        public object MutateIncoming(object message)
        {
            var msg = message as MessageWithDoubleAndByteArray;
            if (msg == null)
                return message;
            //Decompress                
            using (var bigStream = new GZipStream(new MemoryStream(msg.Buffer), CompressionMode.Decompress))
            {
                var bigStreamOut = new MemoryStream();
                bigStream.CopyTo(bigStreamOut);
                msg.Buffer = bigStreamOut.ToArray();
            }
            
            return msg;
        }
    }
}
