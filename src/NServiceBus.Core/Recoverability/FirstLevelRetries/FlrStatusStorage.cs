namespace NServiceBus.Recoverability.FirstLevelRetries
{
    using System;
    using System.Collections.Concurrent;

    //TODO: this needs to be and LRU cache with configurable size
    class FlrStatusStorage
    {
        public void ClearFailuresForMessage(string uniqueMessageId)
        {
            ProcessingFailureInfo processingFailureInfo;
            failuresPerMessage.TryRemove(uniqueMessageId, out processingFailureInfo);
        }

        public void AddFailuresForMessage(string uniqueMessageId, Exception exception)
        {
            failuresPerMessage.AddOrUpdate(
                uniqueMessageId, 
                new ProcessingFailureInfo(1, exception), 
                (s, fi)  => new ProcessingFailureInfo(fi.NumberOfFailures + 1, exception));
        }

        public ProcessingFailureInfo GetFailuresForMessage(string uniqueMessageId)
        {
            ProcessingFailureInfo processingFailureInfo;
            return !failuresPerMessage.TryGetValue(uniqueMessageId, out processingFailureInfo) ? null : processingFailureInfo;
        }

        ConcurrentDictionary<string, ProcessingFailureInfo> failuresPerMessage = new ConcurrentDictionary<string, ProcessingFailureInfo>();
    }
}