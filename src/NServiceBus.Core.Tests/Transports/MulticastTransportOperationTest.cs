using NServiceBus.DelayedDelivery;
using NServiceBus.Transports;

namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class MulticastTransportOperationTest
    {
        [Test]
        public void Should_not_share_constraints_when_not_provided()
        {
            var transportOperation = new MulticastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), typeof(Guid), new Dictionary<string, string>());
            var secondTransportOperation = new MulticastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), typeof(Guid), new Dictionary<string, string>());

            transportOperation.Properties.AsTransportProperties().DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.Zero);

            Assert.IsEmpty(secondTransportOperation.Properties);
            Assert.IsNotEmpty(transportOperation.Properties);
        }
    }
}