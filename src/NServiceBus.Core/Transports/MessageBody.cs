namespace NServiceBus.Transport
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// A reference to a read-only message body.
    /// </summary>
    public class MessageBody : IReadOnlyCollection<byte>
    {
        internal byte[] Bytes;

        /// <summary>
        /// Creates a new instance of <see cref="MessageBody"/>.
        /// </summary>
        public MessageBody(byte[] body)
        {
            Guard.AgainstNull(nameof(body), body);
            Bytes = body;
        }

        /// <inheritdoc />
        public IEnumerator<byte> GetEnumerator() => ((IList<byte>)Bytes).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public int Count => Bytes.Length;

        /// <summary>
        /// Creates a copy of the message body.
        /// </summary>
        public byte[] CreateCopy()
        {
            var copy = new byte[Bytes.Length];
            Buffer.BlockCopy(Bytes, 0, copy, 0, Bytes.Length);
            return copy;
        }

        /// <summary>
        /// Returns a stream of the message body.
        /// </summary>
        public Stream GetStream() => new MemoryStream(Bytes);
    }
}