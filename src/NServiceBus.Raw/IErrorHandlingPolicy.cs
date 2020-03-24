namespace NServiceBus.Raw
{
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Represents a policy for handling errors.
    /// </summary>
    public interface IErrorHandlingPolicy
    {
        /// <summary>
        /// Invoked when an error occurs while processing a message.
        /// </summary>
        /// <param name="handlingContext">Error handling context.</param>
        /// <param name="dispatcher">Dispatcher.</param>
        Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IDispatchMessages dispatcher);
    }
}