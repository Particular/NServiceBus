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
        readonly Func<ErrorContext, IDispatchMessages, Task> customAction;

        /// <summary>
        /// Creates a new <see cref="CustomAction"/> using the provided callback to resolve a message failure.
        /// </summary>
        /// <param name="customAction">The method invoked to handle a failed message.</param>
        public CustomAction(Func<ErrorContext, IDispatchMessages, Task> customAction)
        {
            this.customAction = customAction;
        }

        internal async Task<ErrorHandleResult> Invoke(ErrorContext errorContext, IDispatchMessages dispatcher)
        {
            await customAction(errorContext, dispatcher).ConfigureAwait(false);
            return ErrorHandleResult.Handled;
        }
    }
}