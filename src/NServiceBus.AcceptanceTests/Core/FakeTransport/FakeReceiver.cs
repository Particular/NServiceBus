using System.Collections.Generic;
using System.Threading;
using NServiceBus.Extensibility;
using NServiceBus.Transports;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Threading.Tasks;
    using Settings;
    using Transport;

    //TODO: move the behavior logic to the new seam methods
    class FakeReceiver : IMessageReceiver
    {
        public FakeReceiver(string id)
        {
            Id = id;
        }

        public FakeReceiver(ReadOnlySettings settings)
        {
            this.settings = settings;

            throwCritical = settings.GetOrDefault<bool>("FakeTransport.ThrowCritical");
            throwOnStop = settings.GetOrDefault<bool>("FakeTransport.ThrowOnPumpStop");

            exceptionToThrow = settings.GetOrDefault<Exception>();
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, NServiceBus.CriticalError criticalError, PushSettings pushSettings)
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(IPushMessages)}.{nameof(Init)}");

            this.criticalError = criticalError;
            return Task.FromResult(0);
        }

        public void Start(PushRuntimeSettings limitations)
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(IPushMessages)}.{nameof(Start)}");

            if (throwCritical)
            {
                criticalError.Raise(exceptionToThrow.Message, exceptionToThrow);
            }
        }

        public async Task Stop()
        {
            settings.Get<FakeTransport.StartUpSequence>().Add($"{nameof(IPushMessages)}.{nameof(Stop)}");

            await Task.Yield();

            if (throwOnStop)
            {
                throw exceptionToThrow;
            }
        }

        ReadOnlySettings settings;
        NServiceBus.CriticalError criticalError;
        bool throwCritical;
        bool throwOnStop;
        Exception exceptionToThrow;

        public Task Initialize(PushRuntimeSettings limitations, Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, IReadOnlyCollection<MessageMetadata> events,
            CancellationToken cancellationToken = default)
        {
            //TODO: record this invocation in the startup-sequence
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken = default)
        {
            //TODO: record this invocation in the startup-sequence
            return Task.CompletedTask;
        }

        public Task StopReceive(CancellationToken cancellationToken = default)
        {
            //TODO: record this invocation in the startup-sequence
            return Task.CompletedTask;
        }

        public ISubscriptionManager Subscriptions { get; } = new FakeSubscriptionManager();
        public string Id { get; }

        class FakeSubscriptionManager : ISubscriptionManager
        {
            public Task Subscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}