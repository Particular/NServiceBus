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
        /// <param name="autoDispatch">Creates by default an auto dispatch session. An auto dispatch session dispatches the operation 
        /// immediately to the underlying transport.</param>
        /// <returns>a new <see cref="IBusSession"/> to which all operations performed on it are scoped.</returns>
        IBusSession CreateBusSession(bool autoDispatch = true);
    }
}
