namespace NServiceBus.Unicast
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    /// <summary>
    ///
    /// </summary>
    // Todo make internal in V7
    [ObsoleteEx(TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0")]
    public class CallbackMessageLookup
    {
        /// <summary>
        /// Map of message identifiers to Async Results - useful for cleanup in case of timeouts.
        /// </summary>
        readonly ConcurrentDictionary<string, TaskCompletionSource<CompletionResult>> messageIdToAsyncResultLookup = new ConcurrentDictionary<string, TaskCompletionSource<CompletionResult>>();

        internal void RegisterResult(string messageId, TaskCompletionSource<CompletionResult> taskCompletionSource)
        {
            //TODO: what should we do if the key already exists?
            messageIdToAsyncResultLookup[messageId] = taskCompletionSource;
        }

        internal bool TryGet(string messageId, out TaskCompletionSource<CompletionResult> result)
        {
            return messageIdToAsyncResultLookup.TryRemove(messageId, out result);
        }
    }
}