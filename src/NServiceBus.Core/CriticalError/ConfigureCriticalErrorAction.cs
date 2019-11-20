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
        /// Defines the action that the endpoint performs if a critical error occurs.
        /// </summary>
        /// <param name="endpointConfiguration">The <see cref="EndpointConfiguration" /> to extend.</param>
        /// <param name="onCriticalError">The action to perform.</param>
        public static void DefineCriticalErrorAction(this EndpointConfiguration endpointConfiguration, Func<ICriticalErrorContext, Task> onCriticalError)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(onCriticalError), onCriticalError);
            endpointConfiguration.Settings.Get<HostingComponent.Settings>().CustomCriticalErrorAction = onCriticalError;
        }
    }
}
