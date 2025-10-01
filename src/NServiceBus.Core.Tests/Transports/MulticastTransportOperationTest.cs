namespace NServiceBus.Core.Tests.Transports;

using System;
using NServiceBus.Performance.TimeToBeReceived;
using NUnit.Framework;
using Transport;

[TestFixture]
public class MulticastTransportOperationTest
{
    [Test]
    public void Should_not_share_constraints_when_not_provided()
    {
        var transportOperation = new MulticastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), typeof(Guid), []);
        var secondTransportOperation = new MulticastTransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), typeof(Guid), []);

        transportOperation.Properties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(TimeSpan.FromDays(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondTransportOperation.Properties, Is.Empty);
            Assert.That(transportOperation.Properties, Is.Not.Empty);
        }
    }
}
