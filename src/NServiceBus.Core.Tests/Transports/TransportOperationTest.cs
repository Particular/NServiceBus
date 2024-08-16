namespace NServiceBus.Core.Tests.Transports;

using System;
using NServiceBus.Routing;
using NUnit.Framework;
using Transport;

[TestFixture]
public class TransportOperationTest
{
    [Test]
    public void Should_not_share_constraints_when_not_provided()
    {
        var transportOperation = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination"));
        var secondTransportOperation = new TransportOperation(new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()), new UnicastAddressTag("destination2"));

        Assert.Multiple(() =>
        {
            Assert.That(secondTransportOperation.Properties, Is.Not.SameAs(transportOperation.Properties));
            Assert.That(transportOperation.Properties, Is.Empty);
        });
        Assert.That(secondTransportOperation.Properties, Is.Empty);
    }
}
