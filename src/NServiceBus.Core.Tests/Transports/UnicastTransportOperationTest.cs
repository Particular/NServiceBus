using NServiceBus.DelayedDelivery;

namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class UnicastTransportOperationTest
    {
        [Test]
        public void Should_not_share_constraints_when_not_provided()
        {
            var transportOperation = new UnicastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]),  "destination", new Dictionary<string, string>());
            var secondTransportOperation = new UnicastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), "destination2", new Dictionary<string, string>());

            transportOperation.Properties.Add(typeof(DoNotDeliverBefore).FullName, string.Empty);

            Assert.IsEmpty(secondTransportOperation.Properties);
            Assert.IsNotEmpty(transportOperation.Properties);
        }
    }
}