namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class TransportOperationTest
    {
        [Test]
        public void Should_not_share_constraints_when_not_provided()
        {
            var transportOperation = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), new UnicastAddressTag("destination"));
            var secondTransportOperation = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), new UnicastAddressTag("destination2"));

            Assert.AreNotSame(transportOperation.Properties, secondTransportOperation.Properties);
            Assert.IsEmpty(transportOperation.Properties);
            Assert.IsEmpty(secondTransportOperation.Properties);
        }
    }
}
