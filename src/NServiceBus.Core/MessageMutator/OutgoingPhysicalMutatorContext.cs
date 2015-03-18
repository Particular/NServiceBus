namespace NServiceBus
{
    using System.Collections.Generic;

    /// <summary>
    /// Context class for IMutateOutgoingPhysicalContext
    /// </summary>
    public class OutgoingPhysicalMutatorContext
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="body"></param>
        /// <param name="headers"></param>
        public OutgoingPhysicalMutatorContext(byte[] body, Dictionary<string, string> headers)
        {
            Body = body;
            Headers = headers;
        }

        /// <summary>
        /// The body of the message
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// The headers of the message
        /// </summary>
        public Dictionary<string,string> Headers { get; private set; }
    }
}