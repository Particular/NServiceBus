namespace NServiceBus
{
    using System;
    using System.Threading;
    using NServiceBus.Logging;

    class CriticalErrorHandler
    {
        Action<string, Exception> onCriticalErrorAction;

        public CriticalErrorHandler(Action<string, Exception> onCriticalErrorAction)
        {
            this.onCriticalErrorAction = onCriticalErrorAction;
        }

        public void Handler(string errorMessage, Exception exception)
        {
            LogManager.GetLogger("NServiceBus").Fatal(errorMessage, exception);

            ThreadPool.QueueUserWorkItem(state => onCriticalErrorAction(errorMessage, exception));
        }
    }
}