namespace NServiceBus
{
    using System;
    using System.Threading;
    using Logging;
    using Utils;

    /// <summary>
    ///     Allow override critical error action
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        private static readonly CircuitBreaker CircuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));

        private static Action<string, Exception> onCriticalErrorAction = (errorMessage, exception) => Configure.Instance.Builder.Build<IBus>().Shutdown();

        /// <summary>
        ///     Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        /// <returns></returns>
        public static Configure DefineCriticalErrorAction(this Configure config,
                                                          Action<string, Exception> onCriticalError)
        {
            onCriticalErrorAction = onCriticalError;
            return config;
        }

        /// <summary>
        ///     Execute the configured Critical error action.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The critical exception thrown.</param>
        /// <returns></returns>
        public static Configure OnCriticalError(this Configure config, string errorMessage, Exception exception)
        {
            CircuitBreaker.Execute(() =>
                {
                    LogManager.GetLogger(typeof (ConfigureCriticalErrorAction)).Fatal(errorMessage, exception);
                    ThreadPool.UnsafeQueueUserWorkItem(state => onCriticalErrorAction(errorMessage, exception), null);
                });
            return config;
        }
    }
}