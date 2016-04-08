namespace NServiceBus
{
    using System;
    using System.Runtime.ExceptionServices;

    class ProcessingFailureInfo
    {
        public ProcessingFailureInfo(ExceptionDispatchInfo exceptionDispatchInfo, int flRetries, bool moveToErrorQueue = false, bool deferForSecondLevelRetry = false)
        {
            FLRetries = flRetries;
            ExceptionDispatchInfo = exceptionDispatchInfo;
            MoveToErrorQueue = moveToErrorQueue;
            DeferForSecondLevelRetry = deferForSecondLevelRetry;
        }
        
        public int FLRetries { get; }
        public Exception Exception => ExceptionDispatchInfo.SourceException;
        public ExceptionDispatchInfo ExceptionDispatchInfo { get; }

        public bool MoveToErrorQueue { get; }
        public bool DeferForSecondLevelRetry { get; }

        public static readonly ProcessingFailureInfo NullFailureInfo = new ProcessingFailureInfo(null, 0);
    }
}