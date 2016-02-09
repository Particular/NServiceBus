namespace NServiceBus
{
    using System;

    class ProcessingFailureInfo
    {
        public ProcessingFailureInfo(int numberOfFailedAttempts, Exception exception)
        {
            NumberOfFailedAttempts = numberOfFailedAttempts;
            Exception = exception;
        }

        public int NumberOfFailedAttempts { get; }
        public Exception Exception { get; }
    }
}