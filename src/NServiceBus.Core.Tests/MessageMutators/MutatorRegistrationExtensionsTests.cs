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
                "The specified type is no valid message mutator. Implement one of the following mutator interfaces: NServiceBus.MessageMutator.IMutateIncomingMessages, NServiceBus.MessageMutator.IMutateIncomingTransportMessages, NServiceBus.MessageMutator.IMutateOutgoingMessages, NServiceBus.MessageMutator.IMutateOutgoingTransportMessages",
                exception.Message);
        }

        [Test]
        public void Should_not_throw_when_registering_IMutateIncomingMessage()
        {
            var endpointConfiguration = new EndpointConfiguration("test");

            endpointConfiguration.RegisterMessageMutator(new MyIncomingMessageMutator());
        }

        class MyIncomingMessageMutator : IMutateIncomingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}