namespace NServiceBus.Core.Tests.MessageMutators.MutateTransportMessage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    class MutateOutgoingTransportMessageBehaviorTests
    {
        [Test]
        public async Task Should_not_call_MutateOutgoing_when_hasOutgoingTransportMessageMutators_is_false()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior(new List<IMutateOutgoingTransportMessages>());

            var physicalContext = new TestableOutgoingPhysicalMessageContext();
            physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

            await behavior.Invoke(physicalContext, ctx => TaskEx.CompletedTask);

            var mutator = new MutatorThatIndicatesIfItWasCalled();
            physicalContext.Builder.Register<IMutateOutgoingTransportMessages>(() => mutator);

            await behavior.Invoke(physicalContext, ctx => TaskEx.CompletedTask);

            Assert.IsFalse(mutator.MutateOutgoingCalled);
        }

        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingTransportMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior(new List<IMutateOutgoingTransportMessages>());

            var physicalContext = new TestableOutgoingPhysicalMessageContext();
            physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));
            physicalContext.Builder.Register<IMutateOutgoingTransportMessages>(() => new MutateOutgoingTransportMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(physicalContext, ctx => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        [Test]
        public async Task When_no_mutator_updates_the_body_should_not_update_the_body()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior(new List<IMutateOutgoingTransportMessages>());

            var context = new InterceptUpdateMessageOutgoingPhysicalMessageContext();
            context.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

            context.Builder.Register<IMutateOutgoingTransportMessages>(() => new MutatorWhichDoesNotMutateTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_no_mutator_available_should_not_update_the_body()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior(new List<IMutateOutgoingTransportMessages>());

            var context = new InterceptUpdateMessageOutgoingPhysicalMessageContext();
            context.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

            context.Builder.Register(() => new IMutateOutgoingTransportMessages[] { });

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.False(context.UpdateMessageCalled);
        }

        [Test]
        public async Task When_mutator_modifies_the_body_should_update_the_body()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior(new List<IMutateOutgoingTransportMessages>());

            var context = new InterceptUpdateMessageOutgoingPhysicalMessageContext();
            context.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));

            context.Builder.Register<IMutateOutgoingTransportMessages>(() => new MutatorWhichMutatesTheBody());

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.True(context.UpdateMessageCalled);
        }

        class InterceptUpdateMessageOutgoingPhysicalMessageContext : TestableOutgoingPhysicalMessageContext
        {
            public bool UpdateMessageCalled { get; private set; }

            public override void UpdateMessage(byte[] body)
            {
                base.UpdateMessage(body);

                UpdateMessageCalled = true;
            }
        }

        class MutatorThatIndicatesIfItWasCalled : IMutateOutgoingTransportMessages
        {
            public bool MutateOutgoingCalled { get; set; }

            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                MutateOutgoingCalled = true;

                return TaskEx.CompletedTask;
            }
        }

        class MutateOutgoingMessagesReturnsNull : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                return null;
            }
        }

        class MutatorWhichDoesNotMutateTheBody : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        class MutatorWhichMutatesTheBody : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                context.OutgoingBody = new byte[0];

                return TaskEx.CompletedTask;
            }
        }

        class MutateOutgoingTransportMessagesReturnsNull : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                return null;
            }
        }

        class FakeMessage : IMessage
        {
        }
    }
}