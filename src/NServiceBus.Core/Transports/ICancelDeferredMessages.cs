namespace NServiceBus.Transports
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    /// <summary>
    ///     Allows timeouts to be canceled by the key provided when set.
    /// </summary>
    public interface ICancelDeferredMessages
    {
        /// <summary>
        ///     Clears all timeouts for the given timeout key.
        /// </summary>
        Task CancelDeferredMessages(string messageKey, BehaviorContext context, IPipeInlet<RoutingContext> downpipe);
    }
}