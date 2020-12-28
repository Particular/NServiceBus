using NServiceBus.Transports;

namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using System.Collections.Generic;
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
            var unicastOperation = new TransportOperation(CreateUniqueMessage(), new UnicastAddressTag("destination"), new Dictionary<string, string>(), DispatchConsistency.Isolated);
            var multicastOperation = new TransportOperation(CreateUniqueMessage(), new MulticastAddressTag(typeof(object)),  new Dictionary<string, string>(), DispatchConsistency.Default);
            var operations = new[]
            {
                unicastOperation,
                multicastOperation
            };

            var result = new TransportOperations(operations);

            Assert.AreEqual(1, result.MulticastTransportOperations.Count());
            var multicastOp = result.MulticastTransportOperations.Single();
            Assert.AreEqual(multicastOperation.Message, multicastOp.Message);
            Assert.AreEqual((multicastOperation.AddressTag as MulticastAddressTag)?.MessageType, multicastOp.MessageType);
            Assert.AreEqual(multicastOperation.Properties, multicastOp.Properties.Properties);
            Assert.AreEqual(multicastOperation.RequiredDispatchConsistency, multicastOp.RequiredDispatchConsistency);

            Assert.AreEqual(1, result.UnicastTransportOperations.Count());
            var unicastOp = result.UnicastTransportOperations.Single();
            Assert.AreEqual(unicastOperation.Message, unicastOp.Message);
            Assert.AreEqual((unicastOperation.AddressTag as UnicastAddressTag)?.Destination, unicastOp.Destination);
            Assert.AreEqual(unicastOperation.Properties, unicastOp.Properties.Properties);
            Assert.AreEqual(unicastOperation.RequiredDispatchConsistency, unicastOp.RequiredDispatchConsistency);
        }

        [Test]
        public void When_no_messages_should_return_empty_lists()
        {
            var result = new TransportOperations();

            Assert.AreEqual(0, result.MulticastTransportOperations.Count());
            Assert.AreEqual(0, result.UnicastTransportOperations.Count());
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
            return new OutgoingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new byte[0]);
        }

        class CustomAddressTag : AddressTag
        {
        }
    }
}