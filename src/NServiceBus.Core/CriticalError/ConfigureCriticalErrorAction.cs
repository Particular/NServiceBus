namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allow override critical error action.
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        /// <summary>
        /// Defines the endpoint behavior should a critical error occur.
        /// </summary>
        /// <param name="endpointConfiguration">The <see cref="EndpointConfiguration" /> to extend.</param>
        /// <param name="onCriticalError">Action to perform on critical error.</param>
        public static void DefineCriticalErrorAction(this EndpointConfiguration endpointConfiguration, Func<ICriticalErrorContext, Task> onCriticalError)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(onCriticalError), onCriticalError);
            endpointConfiguration.Settings.Set("onCriticalErrorAction", onCriticalError);
        }
    }
}