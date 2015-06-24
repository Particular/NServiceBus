namespace NServiceBus.Transports
{
    /// <summary>
    /// Abstraction of the capability to dispatch messages.
    /// </summary>
    public interface IDispatchMessages
    {
        /// <summary>
        /// Sends the given <paramref name="message"/>
        /// </summary>
        void Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions);
    }
}