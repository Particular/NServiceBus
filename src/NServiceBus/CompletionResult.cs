using System;

namespace NServiceBus
{
    /// <summary>
    /// The object found in <see cref="IAsyncResult"/>.AsyncState returned by the <see cref="AsyncCallback"/> 
    /// passed to the Send method of the bus.
    /// </summary>
    public class CompletionResult
    {
        /// <summary>
        /// If <see cref="IBus.Return{T}"/> was called, this contains the value passed to it.
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// If <see cref="IBus.Reply(object)"/> was called, this contains the messages passed to it.
        /// </summary>
        public object[] Messages { get; set; }

        /// <summary>
        /// An object that can contain state information for the method.
        /// </summary>
        public object State { get; set; }
    }
}
