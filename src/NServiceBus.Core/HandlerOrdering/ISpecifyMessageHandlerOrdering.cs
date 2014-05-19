namespace NServiceBus
{
    /// <summary>
    /// Specify the order in which message handlers will be invoked.
    /// </summary>
    public interface ISpecifyMessageHandlerOrdering
    {
        /// <summary>
        /// In this method, use the order object to specify the order 
        /// in which message handlers will be activated.
        /// </summary>
        void SpecifyOrder(Order order);
    }
}
