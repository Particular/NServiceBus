namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Performance.TimeToBeReceived;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class MulticastTransportOperationTest
    {
        [Test]
        public void Should_not_share_constraints_when_not_provided()
        {
            var transportOperation = new MulticastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), typeof(Guid));
            var secondTransportOperation = new MulticastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]), typeof(Guid));

            transportOperation.DeliveryConstraints.Add(new DiscardIfNotReceivedBefore(TimeSpan.FromDays(1)));

            Assert.IsEmpty(secondTransportOperation.DeliveryConstraints);
            Assert.IsNotEmpty(transportOperation.DeliveryConstraints);
        }
    }
}