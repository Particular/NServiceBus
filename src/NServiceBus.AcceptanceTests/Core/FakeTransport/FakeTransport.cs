using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Collections.Generic;
    using Transport;

    public class FakeTransport : TransportDefinition
    {
        public class StartUpSequence : List<string> { }

        public FakeTransport() : base(TransportTransactionMode.TransactionScope)
        {
        }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default)
        {
            StartupSequence.Add($"{nameof(TransportDefinition)}.{nameof(Initialize)}");

            var infrastructure = new FakeTransportInfrastructure(StartupSequence, hostSettings, receivers, sendingAddresses,cancellationToken, this);

            infrastructure.ConfigureSendInfrastructure();
            infrastructure.ConfigureReceiveInfrastructure();

            OnTransportInitialize((receivers.Select(r => r.ReceiveAddress).ToArray(), sendingAddresses, hostSettings.SetupInfrastructure));

            return Task.FromResult<TransportInfrastructure>(infrastructure);
        }

        public override string ToTransportAddress(QueueAddress address)
        {
            return new LearningTransport().ToTransportAddress(address);
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

        public StartUpSequence StartupSequence { get; set; } = new StartUpSequence();

        internal Exception ErrorOnReceiverStart { get; set; }
        public void RaiseCriticalErrorOnReceiverStart(Exception exception)
        {
            ErrorOnReceiverStart = exception;
        }

        internal Exception ErrorOnReceiverStop { get; set; }
        public void RaiseExceptionOnReceiverStop(Exception exception)
        {
            ErrorOnReceiverStop = exception;
        }

        internal Exception ErrorOnTransportDispose { get; set; }
        public void RaiseExceptionOnTransportDispose(Exception exception)
        {
            ErrorOnTransportDispose = exception;
        }

        public Action<(string[] receivingAddresses, string[] sendingAddresses, bool setupInfrastructure)> OnTransportInitialize { get; set; } = _ => { };
    }
}