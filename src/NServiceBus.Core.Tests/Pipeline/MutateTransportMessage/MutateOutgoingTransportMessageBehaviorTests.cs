namespace NServiceBus.Core.Tests.Pipeline.MutateTransportMessage
{
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    class MutateOutgoingTransportMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingTransportMessages_MutateOutgoing_returns_null()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior();
            
            var physicalContext = new TestableOutgoingPhysicalMessageContext();
            physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));
            physicalContext.Builder.Register<IMutateOutgoingTransportMessages>(() => new MutateOutgoingTransportMessagesReturnsNull());
            
            Assert.That(async () => await behavior.Invoke(physicalContext, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        class MutateOutgoingTransportMessagesReturnsNull : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                return null;
            }
        }

        [Test]
        public void Should_throw_friendly_exception_when_IMutateOutgoingTransportMessages_MutateOutgoing_modifies_MessageId_header()
        {
            var behavior = new MutateOutgoingTransportMessageBehavior();

            var physicalContext = new TestableOutgoingPhysicalMessageContext();
            physicalContext.Extensions.Set(new OutgoingLogicalMessage(typeof(FakeMessage), new FakeMessage()));
            physicalContext.Builder.Register<IMutateOutgoingTransportMessages>(() => new MutateOutgoingTransportMessagesModifiesMessageIdHeader());

            Assert.That(async () => await behavior.Invoke(physicalContext, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Setting Message Id by manipulating the `NServiceBus.MessageId` header is not supported. Use `sendOptions.SetMessageId(...)` instead."));
        }

        class MutateOutgoingTransportMessagesModifiesMessageIdHeader : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                context.OutgoingHeaders[Headers.MessageId] = "Some new value";
                return TaskEx.CompletedTask;
            }
        }


        class FakeMessage : IMessage { }
    }
}