namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Performance.TimeToBeReceived;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class UnicastTransportOperationTest
    {
        [Test]
        public void Should_not_share_constraints_when_not_provided()
        {
            var transportOperation = new UnicastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], new byte[0]), "destination", []);
            var secondTransportOperation = new UnicastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], new byte[0]), "destination2", []);

            transportOperation.Properties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(TimeSpan.FromDays(1));

            Assert.IsEmpty(secondTransportOperation.Properties);
            Assert.IsNotEmpty(transportOperation.Properties);
        }
    }
}
