namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class CallbackResultEventArgs : EventArgs
    {
        public TaskCompletionSource<CompletionResult> TaskCompletionSource { get; set; }
        public string MessageId { get; set; }
    }
}