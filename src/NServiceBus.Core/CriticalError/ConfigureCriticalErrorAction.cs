namespace NServiceBus
{
    using System;


    /// <summary>
    ///     Allow override critical error action
    /// </summary>
    public static partial class ConfigureCriticalErrorAction
    {

        /// <summary>
        ///     Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="busConfiguration">The <see cref="BusConfiguration"/> to extend.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        public static void DefineCriticalErrorAction(this BusConfiguration busConfiguration, Action<string, Exception> onCriticalError)
        {
            busConfiguration.Settings.Set("onCriticalErrorAction", onCriticalError);
        }

    }
}
