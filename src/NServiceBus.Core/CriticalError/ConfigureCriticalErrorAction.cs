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
        /// <param name="configurationBuilder">The <see cref="ConfigurationBuilder"/> to extend.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        public static void DefineCriticalErrorAction(this ConfigurationBuilder configurationBuilder, Action<string, Exception> onCriticalError)
        {
            configurationBuilder.settings.Set("onCriticalErrorAction", onCriticalError);
        }

    }
}