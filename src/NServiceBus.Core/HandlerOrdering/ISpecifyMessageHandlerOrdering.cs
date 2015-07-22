namespace NServiceBus
{
    /// <summary>
    /// Specify the order in which message handlers will be invoked.
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0", ReplacementTypeOrMember = "BusConfiguration.ExecuteTheseHandlersFirst")]
    public interface ISpecifyMessageHandlerOrdering
    {
        /// <summary>
        /// In this method, use the order object to specify the order 
        /// in which message handlers will be activated.
        /// </summary>
        void SpecifyOrder(Order order);
    }
}
