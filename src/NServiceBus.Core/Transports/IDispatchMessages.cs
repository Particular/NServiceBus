namespace NServiceBus.Transports
{
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction of the capability to dispatch messages.
    /// </summary>
    public interface IDispatchMessages
    {
        /// <summary>
        /// Sends the given <paramref name="message"/>.
        /// </summary>
        Task Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions);
    }
}