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

        /// <summary>
        /// Registers a callback to be invoked when a response arrives to the message sent.
        /// The return code is cast to the given enumerated type - T.
        /// </summary>
        /// <typeparam name="T">An enumeration type or an integer.</typeparam>
        /// <param name="callback"></param>
        void Register<T>(Action<T> callback);

        /// <summary>
        /// Registers a callback to be invoked when a response arrives to the message sent.
        /// The return code is cast to the given enumerated type - T.
        /// Pass either a System.Web.UI.Page or a System.Web.Mvc.AsyncController as the synchronizer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        /// <param name="synchronizer"></param>
        void Register<T>(Action<T> callback, object synchronizer);
    }
}
