namespace NServiceBus
{
    /// <summary>
    /// Specify the name of the endpoint that will be used as the name of the installed Windows Service
    /// instead of the default name.
    /// </summary>
    public interface ISpecifyEndpointName
    {
        /// <summary>
        /// The name of the installed windows service.
        /// </summary>
        string EndpointName { get; }
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
