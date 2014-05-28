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
        private static Action<string, Exception> onCriticalErrorAction = (errorMessage, exception) =>
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
        public static Configure DefineCriticalErrorAction(this Configure config,
                                                          Action<string, Exception> onCriticalError)
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

        /// <summary>
        /// Sets the function to be used when critical error occurs
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="onCriticalError">Assigns the action to perform on critical error.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Replacement = "DefineCriticalErrorAction(this Configure config, Action<string, Exception> onCriticalError)", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure DefineCriticalErrorAction(this Configure config, Action onCriticalError)
        {
            onCriticalErrorAction = (s, exception) => onCriticalError();
            return config;
        }
        /// <summary>
        /// Execute the configured Critical error action
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        [ObsoleteEx(Replacement = "RaiseCriticalError(this Configure config, string errorMessage, Exception exception)", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public static Configure OnCriticalError(this Configure config)
        {
            onCriticalErrorAction("A critical error occurred.", new Exception());
            return config;
        }
    }
}