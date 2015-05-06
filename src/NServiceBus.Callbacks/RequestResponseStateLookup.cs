namespace NServiceBus.Callbacks
{
    using System.Collections.Concurrent;

    class RequestResponseStateLookup
    {
        readonly ConcurrentDictionary<string, TaskCompletionSourceAdapter> messageIdToCompletionSource = new ConcurrentDictionary<string, TaskCompletionSourceAdapter>();

        public void RegisterState(string messageId, TaskCompletionSourceAdapter state)
        {
            messageIdToCompletionSource[messageId] = state;
        }

        public bool TryGet(string messageId, out TaskCompletionSourceAdapter state)
        {
            return messageIdToCompletionSource.TryRemove(messageId, out state);
        }
    }
}