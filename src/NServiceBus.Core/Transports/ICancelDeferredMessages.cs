namespace NServiceBus.Transport
{
    using System.Threading.Tasks;
    using Pipeline;

    /// <summary>
    /// Allows timeouts to be canceled by the key provided when set.
    /// </summary>
    public interface ICancelDeferredMessages
    {
        /// <summary>
        /// Clears all timeouts for the given timeout key.
        /// </summary>
        Task CancelDeferredMessages(string messageKey, IBehaviorContext context);
    }
}