namespace NServiceBus.Core.Tests.Pipeline.MutateInstanceMessage
{
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;
    using Unicast.Messages;

    [TestFixture]
    class MutateIncomingMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateIncomingMessages_MutateIncoming_returns_null()
        {
            var behavior = new MutateIncomingMessageBehavior();

            var logicalMessage = new LogicalMessage(new MessageMetadata(typeof(TestMessage)), new TestMessage());

            var context = new TestableIncomingLogicalMessageContext
            {
                Message = logicalMessage
            };

            context.Builder.Register<IMutateIncomingMessages>(() => new MutateIncomingMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        class MutateIncomingMessagesReturnsNull : IMutateIncomingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                return null;
            }
        }

        class TestMessage : IMessage
        { }
    }
}
