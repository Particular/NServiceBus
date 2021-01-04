using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Settings;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Collections.Generic;
    using Transport;

    public class FakeTransport : TransportDefinition
    {
        StartUpSequence startupSequence;

        public class StartUpSequence : List<string> { }

        public FakeTransport() : base(TransportTransactionMode.TransactionScope)
        {
            startupSequence = new StartUpSequence();
        }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            startupSequence.Add($"{nameof(TransportDefinition)}.{nameof(Initialize)}");

            var infrastructure = new FakeTransportInfrastructure(startupSequence, hostSettings, receivers, sendingAddresses,cancellationToken);

            infrastructure.ConfigureSendInfrastructure();
            infrastructure.ConfigureReceiveInfrastructure();

            return Task.FromResult<TransportInfrastructure>(infrastructure);
        }

        public override string ToTransportAddress(QueueAddress address)
        {
            return address.ToString();
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
        {
            return new[]
            {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive,
                TransportTransactionMode.TransactionScope
            };
        }

        public override bool SupportsDelayedDelivery { get; } = true;
        public override bool SupportsPublishSubscribe { get; } = true;
        public override bool SupportsTTBR { get; } = false;

        public void RaiseCriticalErrorDuringStartup(Exception exception)
        {

        }

        public void RaiseExceptionDuringPumpStop(Exception exception)
        {

        }

        public void RaiseExceptionDuringInfrastructureStop(Exception exception)
        {
        }

        public void WhenQueuesCreated(Action<QueueBindings> onQueueCreation)
        {
        }

        public void CollectStartupSequence(FakeTransport.StartUpSequence startUpSequence)
        {
        }

        public void AssertSettings(Action<ReadOnlySettings> assertion)
        {
        }
    }
}