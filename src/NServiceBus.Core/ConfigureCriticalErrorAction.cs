namespace NServiceBus
{
    using System;
    using Logging;

    /// <summary>
    ///     Allow override critical error action
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        /// <summary>
        ///     Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DefineCriticalErrorAction(this Configure config, Action<string, Exception> onCriticalError)
        {
            config.onCriticalErrorAction = onCriticalError;
            return config;
        }

        /// <summary>
        ///     Execute the configured Critical error action.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The critical exception thrown.</param>
        public static void RaiseCriticalError(this Configure config, string errorMessage, Exception exception)
        {
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            config.onCriticalErrorAction(errorMessage, exception);
        }

    }
}