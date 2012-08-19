namespace NServiceBus.Gateway.Installation
{
    using Unicast.Queuing;

    /// <summary>
    /// Signals to create the Gateway queue.
    /// </summary>
    public class GatewayQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        public Address Address { get { return ConfigureGateway.GatewayInputAddress; } }

        /// <summary>
        /// True if no need to create queue
        /// </summary>
        public bool IsDisabled { get { return ConfigureGateway.GatewayInputAddress == null; } }
    }
}