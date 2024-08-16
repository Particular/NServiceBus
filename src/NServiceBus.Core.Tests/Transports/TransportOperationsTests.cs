namespace NServiceBus.Core.Tests.Transports;

using System;
using System.Linq;
using NServiceBus.Routing;
using Transport;
using NUnit.Framework;

[TestFixture]
public class TransportOperationsTests
{
    [Test]
    public void Should_split_multicast_and_unicast_messages()
    {
        var unicastOperation = new TransportOperation(CreateUniqueMessage(), new UnicastAddressTag("destination"), [], DispatchConsistency.Isolated);
        var multicastOperation = new TransportOperation(CreateUniqueMessage(), new MulticastAddressTag(typeof(object)), [], DispatchConsistency.Default);
        var operations = new[]
        {
            unicastOperation,
            multicastOperation
        };

        var result = new TransportOperations(operations);

        Assert.That(result.MulticastTransportOperations.Count, Is.EqualTo(1));
        var multicastOp = result.MulticastTransportOperations.Single();
        Assert.That(multicastOp.Message, Is.EqualTo(multicastOperation.Message));
        Assert.That(multicastOp.MessageType, Is.EqualTo((multicastOperation.AddressTag as MulticastAddressTag)?.MessageType));
        Assert.That(multicastOp.Properties, Is.EqualTo(multicastOperation.Properties));
        Assert.That(multicastOp.RequiredDispatchConsistency, Is.EqualTo(multicastOperation.RequiredDispatchConsistency));

        Assert.That(result.UnicastTransportOperations.Count, Is.EqualTo(1));
        var unicastOp = result.UnicastTransportOperations.Single();
        Assert.That(unicastOp.Message, Is.EqualTo(unicastOperation.Message));
        Assert.That(unicastOp.Destination, Is.EqualTo((unicastOperation.AddressTag as UnicastAddressTag)?.Destination));
        Assert.That(unicastOp.Properties, Is.EqualTo(unicastOperation.Properties));
        Assert.That(unicastOp.RequiredDispatchConsistency, Is.EqualTo(unicastOperation.RequiredDispatchConsistency));
    }

    [Test]
    public void When_no_messages_should_return_empty_lists()
    {
        var result = new TransportOperations();

        Assert.That(result.MulticastTransportOperations.Count, Is.EqualTo(0));
        Assert.That(result.UnicastTransportOperations.Count, Is.EqualTo(0));
    }

    [Test]
    public void When_providing_unsupported_addressTag_should_throw()
    {
        var transportOperation = new TransportOperation(
            CreateUniqueMessage(),
            new CustomAddressTag(),
            null,
            DispatchConsistency.Default);

        Assert.Throws<ArgumentException>(() => new TransportOperations(transportOperation));
    }

    static OutgoingMessage CreateUniqueMessage()
    {
        return new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>());
    }

    class CustomAddressTag : AddressTag
    {
    }
}