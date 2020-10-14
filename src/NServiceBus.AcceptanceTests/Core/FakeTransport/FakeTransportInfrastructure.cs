namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
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

        public override Task<IPushMessages> CreateReceiver(ReceiveSettings receiveSettings)
        {
            fakeTransportSettings.StartUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(CreateReceiver)}");

            return Task.FromResult<IPushMessages>(new FakeReceiver(fakeTransportSettings,
                settings.CriticalErrorAction));
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