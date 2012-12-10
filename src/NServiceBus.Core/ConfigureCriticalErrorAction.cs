namespace NServiceBus
{
    using System;
    using Logging;

    /// <summary>
    /// Allow override critical error action
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        private static Action<string, Exception> onCriticalErrorAction = (errorMessage, exception) =>
            {
                logger.Fatal(errorMessage, exception);
                Configure.Instance.Builder.Build<IBus>().Shutdown();
            };
            
        /// <summary>
        /// Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="onCriticalError"></param>
        /// <returns></returns>
        public static Configure DefineCriticalErrorAction(this Configure config, Action<string, Exception> onCriticalError)
        {
            onCriticalErrorAction = onCriticalError;
            return config;
        }

        /// <summary>
        /// Execute the configured Critical error action.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The critical exception thrown.</param>
        /// <returns></returns>
        public static Configure OnCriticalError(this Configure config, string errorMessage, Exception exception)
        {
            onCriticalErrorAction(errorMessage, exception);
            return config;
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof (ConfigureCriticalErrorAction));
    }
}
