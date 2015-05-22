namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// The interface used for starting and stopping an IBus.
    /// </summary>
    public interface IStartableBus : IBus
    {
        /// <summary>
        /// Starts the bus and returns a reference to it.
        /// </summary>
        /// <returns>A reference to the bus.</returns>
        Task<IBus> StartAsync();
    }
}