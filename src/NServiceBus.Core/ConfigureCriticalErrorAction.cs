namespace NServiceBus
{
    using System;
    using System.Threading;
    using Logging;

    /// <summary>
    ///     Allow override critical error action
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        static Action<string, Exception> onCriticalErrorAction = (errorMessage, exception) =>
            {
                if (!Configure.BuilderIsConfigured())
                    return;

                if (!Configure.Instance.Configurer.HasComponent<IBus>())
                    return;

                Configure.Instance.Builder.Build<IStartableBus>()
                    .Shutdown();
            };

        /// <summary>
        ///     Sets the function to be used when critical error occurs.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        /// <returns>The configuration object.</returns>
        public static Configure DefineCriticalErrorAction(this Configure config, Action<string, Exception> onCriticalError)
        {
            onCriticalErrorAction = onCriticalError;
            return config;
        }

        /// <summary>
        ///     Execute the configured Critical error action. The action will be performed on a separate thread
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The critical exception thrown.</param>
        public static void RaiseCriticalError(this Configure config, string errorMessage, Exception exception)
        {
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            ThreadPool.QueueUserWorkItem(state =>onCriticalErrorAction(errorMessage, exception));
        }

    }
}