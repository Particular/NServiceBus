namespace NServiceBus.Transports
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// Allows timeouts to be canceled by the key provided when set
    /// </summary>
    public interface ICancelDeferredMessages
    {
        /// <summary>
        /// Clears all timeouts for the given timeout key
        /// </summary>
        void CancelDeferredMessages(string messageKey,BehaviorContext context);
    }
}