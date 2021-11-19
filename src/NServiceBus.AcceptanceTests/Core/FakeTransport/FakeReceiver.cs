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

        public FakeReceiver(string id, string receiveAddress, FakeTransport transportSettings, FakeTransport.StartUpSequence startupSequence,
            Action<string, Exception, CancellationToken> criticalErrorAction)
        {
            this.transportSettings = transportSettings;
            this.startupSequence = startupSequence;
            this.criticalErrorAction = criticalErrorAction;
            Id = id;
            ReceiveAddress = receiveAddress;
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
        {
            startupSequence.Add($"{nameof(IMessageReceiver)}.{nameof(Initialize)} for receiver {Id}");
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken = default)
        {
            startupSequence.Add($"{nameof(IMessageReceiver)}.{nameof(StartReceive)} for receiver {Id}");

            if (transportSettings.ErrorOnReceiverStart != null)
            {
                criticalErrorAction(transportSettings.ErrorOnReceiverStart.Message,
                    transportSettings.ErrorOnReceiverStart, cancellationToken);
            }

            return Task.CompletedTask;
        }

        public async Task StopReceive(CancellationToken cancellationToken = default)
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
        public string ReceiveAddress { get; private set; }

        class FakeSubscriptionManager : ISubscriptionManager
        {
            public Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}