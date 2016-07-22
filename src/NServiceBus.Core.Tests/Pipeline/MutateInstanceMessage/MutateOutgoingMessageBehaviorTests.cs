namespace NServiceBus.Core.Tests.Pipeline.MutateInstanceMessage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    class MutateOutgoingMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new TestableOutgoingLogicalMessageContext();
            context.Extensions.Set(new IncomingMessage("messageId", new Dictionary<string, string>(), new byte[0]));
            context.Extensions.Set(new LogicalMessage(null, null));
            context.Builder.Register<IMutateOutgoingMessages>(() => new MutateOutgoingMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        class MutateOutgoingMessagesReturnsNull : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                return null;
            }
        }
    }
}