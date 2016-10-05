namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allow override critical error action.
    /// </summary>
    public static partial class ConfigureCriticalErrorAction
    {
        /// <summary>
        /// Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="endpointConfiguration">The <see cref="EndpointConfiguration" /> to extend.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        public static void DefineCriticalErrorAction(this EndpointConfiguration endpointConfiguration, Func<ICriticalErrorContext, Task> onCriticalError)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(onCriticalError), onCriticalError);
            endpointConfiguration.Settings.Set("onCriticalErrorAction", onCriticalError);
        }
    }
}