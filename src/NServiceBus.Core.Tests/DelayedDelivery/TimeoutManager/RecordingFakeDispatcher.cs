namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
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

        public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
        {
            DispatchedMessages.Add(new DispatchedMessage(outgoingMessages, context));
            return TaskEx.CompletedTask;
        }
    }
}