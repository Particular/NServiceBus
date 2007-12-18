using System;

namespace NServiceBus.Async
{
	/// <summary>
    /// The object found in <see cref="IAsyncResult"/>.AsyncState returned by the <see cref="AsyncCallback"/> 
    /// passed to the Send method of the bus.
	/// </summary>
	public class CompletionResult
    {
        /// <summary>
        /// The value passed as a parameter to Bus.Return on the server side.
        /// </summary>
        public int errorCode;

        /// <summary>
        /// An object that can contain state information for the method.
        /// </summary>
        public object state;
    }
}
