namespace NServiceBus.Core.Tests.MessageMutators.MutateTransportMessage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MutateIncomingTransportMessageBehaviorTests
    {
        [Test]
        public async Task Should_invoke_all_explicit_mutators()
        {
            var mutator = new MutatorThatIndicatesIfItWasCalled();
            var otherMutator = new MutatorThatIndicatesIfItWasCalled();

            var behavior = new MutateIncomingTransportMessageBehavior(new HashSet<IMutateIncomingTransportMessages> { mutator, otherMutator });

            var context = new TestableIncomingPhysicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.True(mutator.MutateIncomingCalled);
            Assert.True(otherMutator.MutateIncomingCalled);
        }

        [Test]
        public async Task Should_invoke_both_explicit_and_container_provided_mutators()
        {
            var explicitMutator = new MutatorThatIndicatesIfItWasCalled();
            var containerMutator = new MutatorThatIndicatesIfItWasCalled();

            var behavior = new MutateIncomingTransportMessageBehavior(new HashSet<IMutateIncomingTransportMessages> { explicitMutator });

            var context = new TestableIncomingPhysicalMessageContext();
            context.Builder.Register<IMutateIncomingTransportMessages>(() => containerMutator);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.True(explicitMutator.MutateIncomingCalled);
            Assert.True(containerMutator.MutateIncomingCalled);
        }

        [Test]
        public async Task Should_not_call_MutateIncoming_when_hasIncomingTransportMessageMutators_is_false()
        {
            var behavior = new MutateIncomingTransportMessageBehavior(new HashSet<IMutateIncomingTransportMessages>());

            var context = new TestableIncomingPhysicalMessageContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            var mutator = new MutatorThatIndicatesIfItWasCalled();
            context.Builder.Register<IMutateIncomingTransportMessages>(() => mutator);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.IsFalse(mutator.MutateIncomingCalled);
        }

        [Test]
        public void Should_throw_friendly_exception_when_IMutateIncomingTransportMessages_MutateIncoming_returns_null()
        {
            var behavior = new MutateIncomingTransportMessageBehavior(new HashSet<IMutateIncomingTransportMessages>());

            var context = new TestableIncomingPhysicalMessageContext();

            context.Builder.Register<IMutateIncomingTransportMessages>(() => new MutateIncomingTransportMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, ctx => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        [Test]
        public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
        {
            var behavior = new MutateIncomingTransportMessageBehavior(new HashSet<IMutateIncomingTransportMessages>());

            var context = new InterceptUpdateMessageIncomingPhysicalMessageContext();

            context.Builder.Register<IMutateIncomingTransportMessages>(() => new MutatorWhichDoesNotMutateTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageBodyCalled);
        }

        [Test]
        public async Task When_no_mutator_available_should_not_update_the_body()
        {
            var behavior = new MutateIncomingTransportMessageBehavior(new HashSet<IMutateIncomingTransportMessages>());

            var context = new InterceptUpdateMessageIncomingPhysicalMessageContext();

            context.Builder.Register(() => new IMutateIncomingTransportMessages[]{ });

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageBodyCalled);
        }

        [Test]
        public async Task When_mutator_modifies_the_body_should_update_the_body()
        {
            var behavior = new MutateIncomingTransportMessageBehavior(new HashSet<IMutateIncomingTransportMessages>());

            var context = new InterceptUpdateMessageIncomingPhysicalMessageContext();

            context.Builder.Register<IMutateIncomingTransportMessages>(() => new MutatorWhichMutatesTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.True(context.UpdateMessageBodyCalled);
        }

        class InterceptUpdateMessageIncomingPhysicalMessageContext : TestableIncomingPhysicalMessageContext
        {
            public bool UpdateMessageBodyCalled { get; private set; }

            public override void UpdateMessage(byte[] body)
            {
                base.UpdateMessage(body);

                UpdateMessageBodyCalled = true;
            }
        }

        class MutatorThatIndicatesIfItWasCalled : IMutateIncomingTransportMessages
        {
            public bool MutateIncomingCalled { get; set; }

            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                MutateIncomingCalled = true;

                return TaskEx.CompletedTask;
            }
        }

        class MutateIncomingTransportMessagesReturnsNull : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                return null;
            }
        }

        class MutatorWhichDoesNotMutateTheBody : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        class MutatorWhichMutatesTheBody : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                context.Body = new byte[0];

                return TaskEx.CompletedTask;
            }
        }
    }
}
