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
                "The given instance is no valid message mutator. Implement one of the following mutator interfaces: NServiceBus.MessageMutator.IMutateIncomingMessages, NServiceBus.MessageMutator.IMutateIncomingTransportMessages, NServiceBus.MessageMutator.IMutateOutgoingMessages, NServiceBus.MessageMutator.IMutateOutgoingTransportMessages",
                exception.Message);
        }

        [Theory]
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