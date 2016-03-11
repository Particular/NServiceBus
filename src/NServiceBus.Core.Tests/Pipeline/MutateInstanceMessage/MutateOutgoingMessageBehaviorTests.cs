namespace NServiceBus.Core.Tests.Pipeline.MutateInstanceMessage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    class MutateOutgoingMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var message = new FakeMessage();

            var context = new OutgoingLogicalMessageContext("messageId", new Dictionary<string, string>(), new OutgoingLogicalMessage(message.GetType(), message), new List<RoutingStrategy>(), null);

            var builder = new FuncBuilder();

            builder.Register<IMutateOutgoingMessages>(() => new MutateOutgoingMessagesReturnsNull());

            context.Set<IBuilder>(builder);

            Assert.That(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        class MutateOutgoingMessagesReturnsNull : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                return null;
            }
        }

        class FakeMessage : IMessage
        { }
    }
}
