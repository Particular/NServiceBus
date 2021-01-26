namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;
    using Unicast.Messages;

    class FakeReceiver : IMessageReceiver
    {
        readonly FakeTransport transportSettings;
        readonly FakeTransport.StartUpSequence startupSequence;
        readonly Action<string, Exception, CancellationToken> criticalErrorAction;

        public FakeReceiver(string id, FakeTransport transportSettings, FakeTransport.StartUpSequence startupSequence,
            Action<string, Exception, CancellationToken> criticalErrorAction)
        {
            this.transportSettings = transportSettings;
            this.startupSequence = startupSequence;
            this.criticalErrorAction = criticalErrorAction;
            Id = id;
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken)
        {
            startupSequence.Add($"{nameof(IMessageReceiver)}.{nameof(Initialize)} for receiver {Id}");
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken)
        {
            startupSequence.Add($"{nameof(IMessageReceiver)}.{nameof(StartReceive)} for receiver {Id}");

            if (transportSettings.ErrorOnReceiverStart != null)
            {
                criticalErrorAction(transportSettings.ErrorOnReceiverStart.Message,
                    transportSettings.ErrorOnReceiverStart, cancellationToken);
            }

            return Task.CompletedTask;
        }

        public async Task StopReceive(CancellationToken cancellationToken)
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
            public Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}