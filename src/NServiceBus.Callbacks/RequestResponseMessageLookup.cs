namespace NServiceBus.Callbacks
{
    using System.Collections.Concurrent;

    class RequestResponseMessageLookup
    {
        readonly ConcurrentDictionary<string, object> messageIdToAsyncResultLookup = new ConcurrentDictionary<string, object>();

        internal void RegisterResult(string messageId, object tcs)
        {
            messageIdToAsyncResultLookup[messageId] = tcs;
        }

        internal bool TryGet(string messageId, out object result)
        {
            return messageIdToAsyncResultLookup.TryRemove(messageId, out result);
        }
    }
}