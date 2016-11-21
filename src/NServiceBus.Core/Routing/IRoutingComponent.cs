namespace NServiceBus.Features
{
    using System;
    using ObjectBuilder;
    using Routing;
    using Transport;

    /// <summary>
    /// NServiceBus core routing component.
    /// </summary>
    public interface IRoutingComponent
    {
        /// <summary>
        /// Configures routign for sends.
        /// </summary>
        UnicastRoutingTable Sending { get; }

        /// <summary>
        /// Configures routing for publishes.
        /// </summary>
        UnicastSubscriberTable Publishing { get; }

        /// <summary>
        /// Configures mapping of endpoints to endpoint instances.
        /// </summary>
        EndpointInstances EndpointInstances { get; }

        /// <summary>
        /// Registers a subscription handler.
        /// </summary>
        void RegisterSubscriptionHandler(Func<IBuilder, IManageSubscriptions> handlerFactory);
    }
}