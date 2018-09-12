namespace NServiceBus.Core.Tests.MessageMutators
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using NUnit.Framework;

    [TestFixture]
    public class MutatorRegistrationExtensionsTests
    {
        [Test]
        public void Should_throw_ArgumentException_when_registering_non_mutator_type()
        {
            var endpointConfiguration = new EndpointConfiguration("test");

            var exception = Assert.Throws<ArgumentException>(() => endpointConfiguration.RegisterMessageMutator(new object()));
            StringAssert.Contains(
                "The given instance is not a valid message mutator. Implement one of the following mutator interfaces: NServiceBus.MessageMutator.IMutateIncomingMessages, NServiceBus.MessageMutator.IMutateIncomingTransportMessages, NServiceBus.MessageMutator.IMutateOutgoingMessages or NServiceBus.MessageMutator.IMutateOutgoingTransportMessages",
                exception.Message);
        }

        [TestCase(typeof(IncomingMessageMutator))]
        [TestCase(typeof(IncomingTransportMessageMutator))]
        [TestCase(typeof(OutgoingMessageMutator))]
        [TestCase(typeof(OutgoingTransportMessageMutator))]
        public void Should_not_throw_when_registering_mutator(Type mutatorType)
        {
            var endpointConfiguration = new EndpointConfiguration("test");
            var messageMutator = Activator.CreateInstance(mutatorType);

            Assert.DoesNotThrow(() => endpointConfiguration.RegisterMessageMutator(messageMutator));
        }

        [TestCase(typeof(IncomingMessageMutator))]
        [TestCase(typeof(IncomingTransportMessageMutator))]
        [TestCase(typeof(OutgoingMessageMutator))]
        [TestCase(typeof(OutgoingTransportMessageMutator))]
        public void Should_only_invoke_instances_once_even_if_registered_multiple_times(Type mutatorType)
        {
            var endpointConfiguration = new EndpointConfiguration("test");
            var messageMutator = Activator.CreateInstance(mutatorType);

            endpointConfiguration.RegisterMessageMutator(messageMutator);
            endpointConfiguration.RegisterMessageMutator(messageMutator);

            var registry = endpointConfiguration.Settings.Get<NServiceBus.Features.Mutators.RegisteredMutators>();
            
            if (mutatorType == typeof(IncomingMessageMutator))
            {
                Assert.AreEqual(1, registry.IncomingMessage.Count);
            }

            if (mutatorType == typeof(IncomingTransportMessageMutator))
            {
                Assert.AreEqual(1, registry.IncomingTransportMessage.Count);
            }

            if (mutatorType == typeof(OutgoingMessageMutator))
            {
                Assert.AreEqual(1, registry.OutgoingMessage.Count);
            }

            if (mutatorType == typeof(OutgoingTransportMessageMutator))
            {
                Assert.AreEqual(1, registry.OutgoingTransportMessage.Count);
            }
        }

        [Test]
        public void Should_not_throw_when_registering_mutator_implementing_multiple_mutator_interfaces()
        {
            var endpointConfiguration = new EndpointConfiguration("test");

            Assert.DoesNotThrow(() => endpointConfiguration.RegisterMessageMutator(new MultiMutator()));
        }

        class IncomingMessageMutator : IMutateIncomingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                throw new NotImplementedException();
            }
        }

        class IncomingTransportMessageMutator : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                throw new NotImplementedException();
            }
        }

        class OutgoingMessageMutator : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                throw new NotImplementedException();
            }
        }

        class OutgoingTransportMessageMutator : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                throw new NotImplementedException();
            }
        }

        class MultiMutator : IMutateIncomingMessages, IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages, IMutateOutgoingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                throw new NotImplementedException();
            }

            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                throw new NotImplementedException();
            }

            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                throw new NotImplementedException();
            }

            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}