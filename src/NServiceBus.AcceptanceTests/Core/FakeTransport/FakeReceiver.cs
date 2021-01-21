namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using Extensibility;
    using Unicast.Messages;
    using System;
    using System.Threading.Tasks;
    using Transport;

    class FakeReceiver : IMessageReceiver
    {
        readonly FakeTransport transportSettings;
        readonly FakeTransport.StartUpSequence startupSequence;
        readonly Action<string, Exception> criticalErrorAction;

        public FakeReceiver(string id, FakeTransport transportSettings, FakeTransport.StartUpSequence startupSequence,
            Action<string, Exception> criticalErrorAction)
        {
            this.transportSettings = transportSettings;
            this.startupSequence = startupSequence;
            this.criticalErrorAction = criticalErrorAction;
            Id = id;
        }

        public Task Initialize(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage,
            Func<ErrorContext, Task<ErrorHandleResult>> onError)
        {
            startupSequence.Add($"{nameof(IMessageReceiver)}.{nameof(Initialize)} for receiver {Id}");
            return Task.CompletedTask;
        }

        public Task StartReceive()
        {
            startupSequence.Add($"{nameof(IMessageReceiver)}.{nameof(StartReceive)} for receiver {Id}");

            if (transportSettings.ErrorOnReceiverStart != null)
            {
                criticalErrorAction(transportSettings.ErrorOnReceiverStart.Message,
                    transportSettings.ErrorOnReceiverStart);
            }

            return Task.CompletedTask;
        }

        public async Task StopReceive()
        {
            startupSequence.Add($"{nameof(IMessageReceiver)}.{nameof(StopReceive)} for receiver {Id}");

            await Task.Yield();

            if (transportSettings.ErrorOnReceiverStop != null)
            {
                throw transportSettings.ErrorOnReceiverStop;
            }
        }

        public ISubscriptionManager Subscriptions { get; } = new FakeSubscriptionManager();
        public string Id { get; }

        class FakeSubscriptionManager : ISubscriptionManager
        {
            public Task Subscribe(MessageMetadata eventType, ContextBag context)
            {
                return Task.CompletedTask;
            }

            public Task Unsubscribe(MessageMetadata eventType, ContextBag context)
            {
                return Task.CompletedTask;
            }
        }
    }
}