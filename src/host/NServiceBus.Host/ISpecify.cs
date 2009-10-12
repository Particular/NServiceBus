using System;

namespace NServiceBus.Host
{
    /// <summary>
    /// Container class for sub-specifications.
    /// Implement the contained interfaces on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public class ISpecify
    {
        /// <summary>
        /// Specify the name of the endpoint that will be used as the name of the installed Windows Service
        /// instead of the default name.
        /// </summary>
        public interface EndpointName
        {
            /// <summary>
            /// The name of the installed windows service.
            /// </summary>
            string EndpointName { get; }
        }

		/// <summary>
		/// Tell the topshelf framework to set services to start automatically
		/// </summary>
    	public interface ToStartAutomatically
    	{
    	}
    }

    /// <summary>
    /// Specify the order in which message handlers will be invoked.
    /// </summary>
    public interface ISpecifyMessageHandlerOrdering
    {
        /// <summary>
        /// In this method, use the order object to specify the order in which message handlers will be activated.
        /// </summary>
        /// <param name="order"></param>
        void SpecifyOrder(Order order);
    }
}
