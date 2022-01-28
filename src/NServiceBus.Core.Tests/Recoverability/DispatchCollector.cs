namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    class DispatchCollector
    {
        public IDictionary<string, string> MessageHeaders { get; private set; }
        public TimeSpan? Delay { get; private set; }
        public string Destination { get; private set; }

        public Task Collect(TransportOperation transportOperation, CancellationToken cancellationToken = default)
        {
            var unicastAddressTag = transportOperation.AddressTag as UnicastAddressTag;

            Assert.IsNotNull(unicastAddressTag);

            Destination = unicastAddressTag.Destination;

            MessageHeaders = transportOperation.Message.Headers;

            Delay = transportOperation.Properties.DelayDeliveryWith?.Delay;

            return Task.CompletedTask;
        }
    }
}