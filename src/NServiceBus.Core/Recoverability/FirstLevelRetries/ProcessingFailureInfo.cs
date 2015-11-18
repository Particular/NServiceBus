namespace NServiceBus.Recoverability.FirstLevelRetries
{
    using System;

    class ProcessingFailureInfo
    {
        public ProcessingFailureInfo(int numberOfFailures, Exception exception)
        {
            NumberOfFailures = numberOfFailures;
            Exception = exception;
        }

        public int NumberOfFailures { get; set; }     

        public Exception Exception { get; set; }
    }
}