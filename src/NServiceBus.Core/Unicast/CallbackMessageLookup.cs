namespace NServiceBus.Unicast
{
    using System.Collections.Concurrent;

    /// <summary>
    /// 
    /// </summary>
    public class CallbackMessageLookup
    {
        /// <summary>
        /// Map of message identifiers to Async Results - useful for cleanup in case of timeouts.
        /// </summary>
        readonly ConcurrentDictionary<string, BusAsyncResult> messageIdToAsyncResultLookup = new ConcurrentDictionary<string, BusAsyncResult>();

        internal void RegisterResult(string messageId, BusAsyncResult result)
        {
            //TODO: what should we do if the key already exists?
            messageIdToAsyncResultLookup[messageId] = result;
        }

        internal bool TryGet(string messageId, out BusAsyncResult result)
        {
            return messageIdToAsyncResultLookup.TryRemove(messageId, out result);
        }
    }
}