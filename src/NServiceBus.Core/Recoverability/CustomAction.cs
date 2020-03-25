namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Transport;

    /// <summary>
    /// Defines a custom recoverability action, taking care of handling a message processing error.
    /// </summary>
    public class CustomAction : RecoverabilityAction
    {
        readonly Func<ErrorContext, Task> customAction;

        /// <summary>
        /// Creates a new <see cref="CustomAction"/> using the provided callback to resolve a message failure.
        /// </summary>
        /// <param name="customAction">The method invoked to handle a failed message.</param>
        public CustomAction(Func<ErrorContext, Task> customAction)
        {
            this.customAction = customAction;
        }

        /// <summary>
        /// Invokes the custom recoverability action.
        /// </summary>
        /// <returns>always returns <see cref="ErrorHandleResult.Handled"/> to indicate the transport to consume the message.</returns>
        public async Task<ErrorHandleResult> Invoke(ErrorContext errorContext)
        {
            await customAction(errorContext).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }
    }
}