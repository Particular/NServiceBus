namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;

    public class RecordingFakeDispatcher : IDispatchMessages
    {
        public class DispatchedMessage
        {
            public TransportOperations Operations { get; }
            public ContextBag Context { get; }

            public DispatchedMessage(TransportOperations operations, ContextBag context)
            {
                Operations = operations;
                Context = context;
            }
        }

        public readonly List<DispatchedMessage> DispatchedMessages = new List<DispatchedMessage>();

        public async Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            DispatchedMessages.Add(new DispatchedMessage(outgoingMessages, context));
            await TaskEx.CompletedTask;
        }
    }
}