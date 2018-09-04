namespace NServiceBus.Core.Tests.MessageMutators.MutateInstanceMessage
{
    using System.Collections.Generic;
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
        public async Task Should_not_call_MutateIncoming_when_hasIncomingMessageMutators_is_false()
        {
            var behavior = new MutateIncomingMessageBehavior(new List<IMutateIncomingMessages>());

            var context = new TestableIncomingLogicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            var mutator = new MutatorThatIndicatesIfItWasCalled();
            context.Builder.Register<IMutateIncomingMessages>(() => mutator);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.IsFalse(mutator.MutateIncomingCalled);
        }

        [Test]
        public void Should_throw_friendly_exception_when_IMutateIncomingMessages_MutateIncoming_returns_null()
        {
            var behavior = new MutateIncomingMessageBehavior(new List<IMutateIncomingMessages>());

            var logicalMessage = new LogicalMessage(new MessageMetadata(typeof(TestMessage)), new TestMessage());

            var context = new TestableIncomingLogicalMessageContext
            {
                Message = logicalMessage
            };

            context.Builder.Register<IMutateIncomingMessages>(() => new MutateIncomingMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, ctx => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        [Test]
        public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
        {
            var behavior = new MutateIncomingMessageBehavior(new List<IMutateIncomingMessages>());

            var context = new InterceptUpdateMessageIncomingLogicalMessageContext();

            context.Builder.Register<IMutateIncomingMessages>(() => new MutatorWhichDoesNotMutateTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_no_mutator_available_should_not_update_the_body()
        {
            var behavior = new MutateIncomingMessageBehavior(new List<IMutateIncomingMessages>());

            var context = new InterceptUpdateMessageIncomingLogicalMessageContext();

            context.Builder.Register(() => new IMutateIncomingMessages[] { });

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_mutator_modifies_the_body_should_update_the_body()
        {
            var behavior = new MutateIncomingMessageBehavior(new List<IMutateIncomingMessages>());

            var context = new InterceptUpdateMessageIncomingLogicalMessageContext();

            context.Builder.Register<IMutateIncomingMessages>(() => new MutatorWhichMutatesTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.True(context.UpdateMessageCalled);
        }

        class InterceptUpdateMessageIncomingLogicalMessageContext : TestableIncomingLogicalMessageContext
        {
            public bool UpdateMessageCalled { get; private set; }

            public override void UpdateMessageInstance(object newInstance)
            {
                base.UpdateMessageInstance(newInstance);

                UpdateMessageCalled = true;
            }
        }

        class MutatorThatIndicatesIfItWasCalled : IMutateIncomingMessages
        {
            public bool MutateIncomingCalled { get; set; }

            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                MutateIncomingCalled = true;

                return TaskEx.CompletedTask;
            }
        }

        class MutatorWhichDoesNotMutateTheBody : IMutateIncomingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        class MutatorWhichMutatesTheBody : IMutateIncomingMessages
        {
            public Task MutateIncoming(MutateIncomingMessageContext context)
            {
                context.Message = new object();

                return TaskEx.CompletedTask;
            }
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
