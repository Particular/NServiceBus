namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    public class RecordingFakeDispatcher : IDispatchMessages
    {
        public class DispatchedMessage
        {
            public TransportOperations Operations { get; }
            public TransportTransaction Transaction { get; }

            public DispatchedMessage(TransportOperations operations, TransportTransaction transaction)
            {
                Operations = operations;
                Transaction = transaction;
            }
        }

        public readonly List<DispatchedMessage> DispatchedMessages = new List<DispatchedMessage>();

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction)
        {
            DispatchedMessages.Add(new DispatchedMessage(outgoingMessages, transaction));
            return Task.CompletedTask;
        }
    }
}