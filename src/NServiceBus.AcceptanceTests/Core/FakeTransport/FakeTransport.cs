using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 1998

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System;
    using System.Collections.Generic;
    using Transport;

    public class FakeTransport : TransportDefinition
    {
        public FakeTransport()
        {
            SupportedTransactionModes = new[]
            {
                SupportedTransactionMode ?? TransportTransactionMode.TransactionScope
            };
        }

        public TransportTransactionMode? SupportedTransactionMode { get; set; }

        public List<string> StartUpSequence { get; } = new List<string>();

        public bool ThrowOnInfrastructureStop { get; set; }

        public bool RaiseCriticalErrorDuringStartup { get; set; }

        public bool ThrowOnPumpStop { get; set; }

        public Exception ExceptionToThrow { get; set; } = new Exception();

        public Action<QueueBindings> OnQueueCreation { get; set; }

        public override async Task<TransportInfrastructure> Initialize(Settings settings, ReceiveSettings[] receivers, string[] SendingAddresses, CancellationToken cancellationToken)
        {
            StartUpSequence.Add($"{nameof(TransportDefinition)}.{nameof(Initialize)}");

            ////if (settings.TryGet<Action<ReadOnlySettings>>("FakeTransport.AssertSettings", out var assertion))
            ////{
            ////    assertion(settings);
            ////}

            return new FakeTransportInfrastructure(settings, this, receivers);
        }

        public override string ToTransportAddress(EndpointAddress logicalAddress)
        {
            return logicalAddress.ToString();
        }

        public override IReadOnlyCollection<TransportTransactionMode> SupportedTransactionModes { get; protected set; }

        /// <summary>
        /// </summary>
        public override bool SupportsTTBR { get; } = false;
    }
}