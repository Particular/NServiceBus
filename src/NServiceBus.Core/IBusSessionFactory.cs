namespace NServiceBus
{
    /// <summary>
    /// Provides interface for interacting with the bus.
    /// </summary>
    public interface IBusSessionFactory
    {
        /// <summary>
        /// Creates a <see cref="IBusSession"/> which can be used to access several bus operations like send, publish, subscribe and more.
        /// </summary>
        /// <returns>a new <see cref="IBusSession"/> to which all operations performed on it are scoped.</returns>
        IBusSession CreateBusSession();
    }
}
