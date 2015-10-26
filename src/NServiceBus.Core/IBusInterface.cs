namespace NServiceBus
{
    /// <summary>
    /// Provides interface for interacting with the bus.
    /// </summary>
    public interface IBusInterface
    {
        /// <summary>
        /// Creates a <see cref="IBusContext"/> which can be used to access several bus operations like send, publish, subscribe and more.
        /// </summary>
        /// <returns>a new <see cref="IBusContext"/> to which all operations performed on it are scoped.</returns>
        IBusContext CreateBusContext();
    }
}
