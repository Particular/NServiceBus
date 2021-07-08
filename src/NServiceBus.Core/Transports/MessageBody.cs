namespace NServiceBus.Transport
{
    using System.Collections;
    using System.Collections.Generic;

    //TODO should this implement ICloneable?
    /// <inheritdoc />
    public class MessageBody : IReadOnlyCollection<byte>
    {
        internal byte[] Bytes;

        /// <summary>
        /// Blabla.
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
    }
}