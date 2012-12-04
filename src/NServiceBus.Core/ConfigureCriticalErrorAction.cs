namespace NServiceBus
{
    using System;

    /// <summary>
    /// Allow override critical error action
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        private static Action<string> onCriticalErrorAction = (errorMessage) => Environment.FailFast(string.Format("The following critical error was encountered by NServiceBus:\n{0}\nNServiceBus is shutting down.", errorMessage));
            
        /// <summary>
        /// Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="onCriticalError"></param>
        /// <returns></returns>
        public static Configure DefineCriticalErrorAction(this Configure config, Action<string> onCriticalError)
        {
            onCriticalErrorAction = onCriticalError;
            return config;
        }

        /// <summary>
        /// Execute the configured Critical error action.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public static Configure OnCriticalError(this Configure config, string errorMessage)
        {
            onCriticalErrorAction(errorMessage);
            return config;
        }
    }
}
