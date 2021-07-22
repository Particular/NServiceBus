namespace NServiceBus.Core.Tests.MessageMutators.MutateInstanceMessage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    class MutateOutgoingMessageBehaviorTests
    {
        [Test]
        public async Task Should_invoke_all_explicit_mutators()
        {
            var mutator = new MutatorThatIndicatesIfItWasCalled();
            var otherMutator = new MutatorThatIndicatesIfItWasCalled();

            var behavior = new MutateOutgoingMessageBehavior(new HashSet<IMutateOutgoingMessages> { mutator, otherMutator });

            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.True(mutator.MutateOutgoingCalled);
            Assert.True(otherMutator.MutateOutgoingCalled);
        }

        [Test]
        public async Task Should_invoke_both_explicit_and_container_provided_mutators()
        {
            var explicitMutator = new MutatorThatIndicatesIfItWasCalled();
            var containerMutator = new MutatorThatIndicatesIfItWasCalled();

            var behavior = new MutateOutgoingMessageBehavior(new HashSet<IMutateOutgoingMessages> { explicitMutator });

            var context = new TestableOutgoingLogicalMessageContext();
            context.Services.AddTransient<IMutateOutgoingMessages>(sp => containerMutator);

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.True(explicitMutator.MutateOutgoingCalled);
            Assert.True(containerMutator.MutateOutgoingCalled);
        }

        [Test]
        public async Task Should_not_call_MutateOutgoing_when_hasOutgoingMessageMutators_is_false()
        {
            var behavior = new MutateOutgoingMessageBehavior(new HashSet<IMutateOutgoingMessages>());

            var context = new TestableOutgoingLogicalMessageContext();

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            var mutator = new MutatorThatIndicatesIfItWasCalled();
            context.Services.AddTransient<IMutateOutgoingMessages>(sp => mutator);

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.IsFalse(mutator.MutateOutgoingCalled);
        }

        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingMessageBehavior(new HashSet<IMutateOutgoingMessages>());

            var context = new TestableOutgoingLogicalMessageContext();
            context.Extensions.Set(new IncomingMessage("messageId", new Dictionary<string, string>(), new MessageBody(new byte[0])));
            context.Extensions.Set(new LogicalMessage(null, null));
            context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutateOutgoingMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, ctx => Task.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        [Test]
        public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
        {
            var behavior = new MutateOutgoingMessageBehavior(new HashSet<IMutateOutgoingMessages>());

            var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

            context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutatorWhichDoesNotMutateTheBody());

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_no_mutator_available_should_not_update_the_body()
        {
            var behavior = new MutateOutgoingMessageBehavior(new HashSet<IMutateOutgoingMessages>());

            var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

            context.Services.AddTransient(sp => new IMutateOutgoingMessages[] { });

            await behavior.Invoke(context, ctx => Task.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_mutator_modifies_the_body_should_update_the_body()
        {
            var behavior = new MutateOutgoingMessageBehavior(new HashSet<IMutateOutgoingMessages>());

            var context = new InterceptUpdateMessageOutgoingLogicalMessageContext();

            context.Services.AddTransient<IMutateOutgoingMessages>(sp => new MutatorWhichMutatesTheBody());

            await behavior.Invoke(context, ctx => Task.CompletedTask);

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

                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }
        }

        class MutatorWhichMutatesTheBody : IMutateOutgoingMessages
        {
            public Task MutateOutgoing(MutateOutgoingMessageContext context)
            {
                context.OutgoingMessage = new object();

                return Task.CompletedTask;
            }
        }
    }
}