namespace NServiceBus.Core.Tests.Pipeline.MutateTransportMessage
{
    using System.Threading.Tasks;
    using MessageMutator;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MutateIncomingTransportMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateIncomingTransportMessages_MutateIncoming_returns_null()
        {
            var behavior = new MutateIncomingTransportMessageBehavior();
            
            var context = new TestableIncomingPhysicalMessageContext();

            context.Builder.Register<IMutateIncomingTransportMessages>(() => new MutateIncomingTransportMessagesReturnsNull());

            Assert.That(async () => await behavior.Invoke(context, () => TaskEx.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        class MutateIncomingTransportMessagesReturnsNull : IMutateIncomingTransportMessages
        {
            public Task MutateIncoming(MutateIncomingTransportMessageContext context)
            {
                return null;
            }
        }
    }
}
