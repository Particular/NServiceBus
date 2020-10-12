namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using Extensibility;
    using NServiceBus.Routing;
    using Transport;

    public class FakeTransportInfrastructure : TransportInfrastructure
    {
        public FakeTransportInfrastructure(TransportSettings settings, FakeTransport fakeTransportSettings)
        {
            this.settings = settings;
            this.fakeTransportSettings = fakeTransportSettings;
        }

        public override bool SupportsTTBR { get; } = false;

        public override TransportTransactionMode TransactionMode => 
            fakeTransportSettings.SupportedTransactionMode ?? TransportTransactionMode.TransactionScope;

        public override EndpointAddress BuildLocalAddress(string queueName)
        {
            return new EndpointAddress(string.Empty, null, new Dictionary<string, string>(), null);
        }

        public override string ToTransportAddress(EndpointAddress logicalAddress)
        {
            return logicalAddress.ToString();
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure(ReceiveSettings receiveSettings)
        {
            fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(ConfigureReceiveInfrastructure)}");

            return new TransportReceiveInfrastructure(() => new FakeReceiver(fakeTransportSettings, settings.CriticalErrorAction),
                () => new FakeQueueCreator(fakeTransportSettings),
                () =>
                {
                    fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportReceiveInfrastructure)}.PreStartupCheck");
                    return Task.FromResult(StartupCheckResult.Success);
                });
        }

        public override Task Start()
        {
            fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(Start)}");

            return Task.FromResult(0);

        }
        public override async Task Stop()
        {
            fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(Stop)}");

            await Task.Yield();

            if (fakeTransportSettings.ThrowOnInfrastructureStop)
            {
                throw fakeTransportSettings.ExceptionToThrow;
            }
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(ConfigureSendInfrastructure)}");

            return new TransportSendInfrastructure(() => new FakeDispatcher(),
                () =>
                {
                    fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportSendInfrastructure)}.PreStartupCheck");
                    return Task.FromResult(StartupCheckResult.Success);
                });
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure(SubscriptionSettings subscriptionSettings)
        {
            fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(ConfigureSubscriptionInfrastructure)}");

            return new TransportSubscriptionInfrastructure(()=> new FakeSubscriptionManager());
        }

        TransportSettings settings;
        private readonly FakeTransport fakeTransportSettings;

        class FakeSubscriptionManager : IManageSubscriptions
        {
            public Task Subscribe(Type eventType, ContextBag context)
            {
                return Task.FromResult(0);
            }

            public Task Unsubscribe(Type eventType, ContextBag context)
            {
                return Task.FromResult(0);
            }
        }
    }
}