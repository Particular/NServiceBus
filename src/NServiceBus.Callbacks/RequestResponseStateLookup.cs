namespace NServiceBus.Callbacks
{
    using System.Collections.Concurrent;

    class RequestResponseStateLookup
    {
        readonly ConcurrentDictionary<string, RequestResponse.State> messageIdToAsyncResultLookup = new ConcurrentDictionary<string, RequestResponse.State>();

        internal void RegisterState(string messageId, RequestResponse.State state)
        {
            if (state.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            state.CancellationToken.Register(() =>
            {
                RequestResponse.State s;
                TryGet(messageId, out s);
            });
            messageIdToAsyncResultLookup[messageId] = state;
        }

        internal bool TryGet(string messageId, out RequestResponse.State state)
        {
            return messageIdToAsyncResultLookup.TryRemove(messageId, out state);
        }
    }
}