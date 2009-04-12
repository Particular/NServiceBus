using System;

namespace NServiceBus
{
    /// <summary>
    /// Objects of this interface are returned from calling IBus.Send.
    /// The interface allows the caller to register for a callback when a response
    /// is received to their original call to IBus.Send.
    /// </summary>
    public interface ICallback
    {
        /// <summary>
        /// Registers a callback to be invoked when a response arrives to the message sent.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="state">State that will be passed to the callback method.</param>
        /// <returns>An IAsyncResult useful for integration with ASP.NET async tasks.</returns>
        IAsyncResult Register(AsyncCallback callback, object state);
    }
}
