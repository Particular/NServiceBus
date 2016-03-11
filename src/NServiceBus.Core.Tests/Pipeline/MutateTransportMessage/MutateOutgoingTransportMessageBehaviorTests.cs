namespace NServiceBus.Core.Tests.Pipeline.MutateTransportMessage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    class MutateOutgoingTransportMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingTransportMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior();

            var message = new FakeMessage();

            var outgoingLogicalMessage = new OutgoingLogicalMessage(message.GetType(),message);

            var logicalContext = new OutgoingLogicalMessageContext("messageId", new Dictionary<string, string>(), outgoingLogicalMessage, new List<RoutingStrategy>(), null);

            var physicalContext = new OutgoingPhysicalMessageContext(new byte[0], new List<RoutingStrategy>(), logicalContext);

            physicalContext.Set(outgoingLogicalMessage);

            var builder = new FuncBuilder();

            builder.Register<IMutateOutgoingTransportMessages>(() => new MutateOutgoingTransportMessagesReturnsNull());

            physicalContext.Set<IBuilder>(builder);

            Assert.That(async () => await behavior.Invoke(physicalContext, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        class MutateOutgoingTransportMessagesReturnsNull : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                return null;
            }
        }

        class FakeMessage : IMessage { }
    }
}