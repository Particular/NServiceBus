namespace NServiceBus
{
    using System;
    using System.Runtime.ExceptionServices;

    class TimeoutProcessingFailureInfo
    {
        public TimeoutProcessingFailureInfo(int numberOfFailedAttempts, ExceptionDispatchInfo exceptionDispatchInfo)
        {
            NumberOfFailedAttempts = numberOfFailedAttempts;
            ExceptionDispatchInfo = exceptionDispatchInfo;
        }

        public int NumberOfFailedAttempts { get; }
        public Exception Exception => ExceptionDispatchInfo.SourceException;
        ExceptionDispatchInfo ExceptionDispatchInfo { get; }

        public static readonly TimeoutProcessingFailureInfo NullFailureInfo = new TimeoutProcessingFailureInfo(0, null);
    }
}