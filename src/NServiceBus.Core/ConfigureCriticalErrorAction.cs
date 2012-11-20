namespace NServiceBus
{
    using System;
    using Unicast.Transport;

    /// <summary>
    /// Allow override critical error action
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        /// <summary>
        /// Set default behavior to zeroing the number of receiving worker threads.
        /// </summary>
        private static Action onCriticalErrorAction = () => Configure.Instance.Builder.Build<ITransport>().ChangeNumberOfWorkerThreads(0);
            
        /// <summary>
        /// Sets the function to be used when critical error occurs
        /// </summary>
        /// <param name="config"></param>
        /// <param name="onCriticalError"></param>
        /// <returns></returns>
        public static Configure DefineCriticalErrorAction(this Configure config, Action onCriticalError)
        {
            onCriticalErrorAction = onCriticalError;
            return config;
        }
        /// <summary>
        /// Execute the configured Critical error action
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure OnCriticalError(this Configure config)
        {
            onCriticalErrorAction();
            return config;
        }
    }
}
