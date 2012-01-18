using System;
using Common.Logging;

namespace NServiceBus
{
    /// <summary>
    /// Allow override critical error action
    /// </summary>
    public static class ConfigureCriticalErrorAction
    {
        /// <summary>
        /// The function is used to get the OnCriticalError behavior
        /// </summary>
        private static Action<Exception> onCriticalErrorAction = exception =>
            Logger.FatalFormat("Exception {0} occurred, exception message: {1}.", exception.GetType(), exception.Message);

        /// <summary>
        /// Sets the function to be used when critical error occurs
        /// </summary>
        /// <param name="config"></param>
        /// <param name="onCriticalError"></param>
        /// <returns></returns>
        public static Configure DefineCriticalErrorAction(this Configure config, Action<Exception> onCriticalError)
        {
            onCriticalErrorAction = onCriticalError;
            return config;
        }
        /// <summary>
        /// Execute the configured Critical error action
        /// </summary>
        /// <param name="config"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static Configure OnCriticalError(this Configure config, Exception ex)
        {
            onCriticalErrorAction(ex);
            return config;
        }
        
        static readonly ILog Logger = LogManager.GetLogger("ConfigureCriticalErrorAction");
    }
}
