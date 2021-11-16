namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    public class FakeTransport : TransportDefinition
    {
        public class StartUpSequence : List<string> { }

        public FakeTransport()
            : base(TransportTransactionMode.TransactionScope, true, true, false)
        {
        }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            StartupSequence.Add($"{nameof(TransportDefinition)}.{nameof(Initialize)}");

            var infrastructure = new FakeTransportInfrastructure(StartupSequence, hostSettings, receivers, this);

            infrastructure.ConfigureSendInfrastructure();
            infrastructure.ConfigureReceiveInfrastructure();

            OnTransportInitialize((receivers.Select(r => r.ReceiveAddress).ToArray(), sendingAddresses, hostSettings.SetupInfrastructure));

            return Task.FromResult<TransportInfrastructure>(infrastructure);
        }

        [Obsolete("Obsolete marker to make the code compile", false)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override string ToTransportAddress(QueueAddress address)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
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

        public Action<(QueueAddress[] receivingAddresses, string[] sendingAddresses, bool setupInfrastructure)> OnTransportInitialize { get; set; } = _ => { };
    }
}