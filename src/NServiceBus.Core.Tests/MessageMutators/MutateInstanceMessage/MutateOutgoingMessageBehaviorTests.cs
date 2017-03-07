﻿namespace NServiceBus.Core.Tests.MessageMutators.MutateInstanceMessage
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
        public async Task Should_not_call_MutateOutgoing_when_hasOutgoingMessageMutators_is_false()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            var mutator = new MutatorThatIndicatesIfItWasCalled();
            context.Builder.Register<IMutateOutgoingMessages>(() => mutator);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.IsFalse(mutator.MutateOutgoingCalled);
        }

        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new TestableOutgoingLogicalMessageContext();
            context.Extensions.Set(new IncomingMessage("messageId", new Dictionary<string, string>(), new byte[0]));
            context.Extensions.Set(new LogicalMessage(null, null));
            context.Builder.Register<IMutateOutgoingMessages>(() => new MutateOutgoingMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, ctx => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        [Test]
        public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

            context.Builder.Register<IMutateOutgoingMessages>(() => new MutatorWhichDoesNotMutateTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_no_mutator_available_should_not_update_the_body()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

            context.Builder.Register(() => new IMutateOutgoingMessages[] { });

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_mutator_modifies_the_body_should_update_the_body()
        {
            var behavior = new MutateOutgoingMessageBehavior();

            var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

            context.Builder.Register<IMutateOutgoingMessages>(() => new MutatorWhichMutatesTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.True(context.UpdateMessageCalled);
        }

        class InterceptUpdateMessageOutgoingLogicalMessageContext : TestableOutgoingLogicalMessageContext
        {
            public bool UpdateMessageCalled { get; private set; }

            public override void UpdateMessage(object newInstance)
            {
                base.UpdateMessage(newInstance);

                UpdateMessageCalled = true;
            }
        }

        class MutatorThatIndicatesIfItWasCalled : IMutateOutgoingMessages
        {
            public bool MutateOutgoingCalled { get; set; }

            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                MutateOutgoingCalled = true;

                return TaskEx.CompletedTask;
            }
        }

        class MutateOutgoingMessagesReturnsNull : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                return null;
            }
        }

        class MutatorWhichDoesNotMutateTheBody : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        class MutatorWhichMutatesTheBody : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                context.OutgoingMessage = new object();

                return TaskEx.CompletedTask;
            }
        }
    }
}