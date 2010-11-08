namespace NServiceBus
{
    /// <summary>
    /// Implementers will be called before and after all message handlers.
    /// </summary>
    public interface IMessageModule
    {
        /// <summary>
        /// This method is called before any message handlers are called.
        /// </summary>
        void HandleBeginMessage();

        /// <summary>
        /// This method is called after all message handlers have been called.
        /// </summary>
        void HandleEndMessage();

        /// <summary>
        /// This method is called if there was an error in processing the message,
        /// and may be called after <see cref="HandleEndMessage"/>.
        /// </summary>
        void HandleError();
    }
}
