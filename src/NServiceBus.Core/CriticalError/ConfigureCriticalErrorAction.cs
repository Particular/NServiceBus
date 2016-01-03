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
        /// <param name="busConfiguration">The <see cref="BusConfiguration" /> to extend.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        public static void DefineCriticalErrorAction(this BusConfiguration busConfiguration, Func<CriticalErrorContext, Task> onCriticalError)
        {
            Guard.AgainstNull(nameof(busConfiguration), busConfiguration);
            Guard.AgainstNull(nameof(onCriticalError), onCriticalError);
            busConfiguration.Settings.Set("onCriticalErrorAction", onCriticalError);
        }
    }
}