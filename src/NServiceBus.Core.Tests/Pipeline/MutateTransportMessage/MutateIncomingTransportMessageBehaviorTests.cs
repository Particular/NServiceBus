namespace NServiceBus.Core.Tests.Pipeline.MutateTransportMessage
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using ObjectBuilder;

    [TestFixture]
    public class MutateIncomingTransportMessageBehaviorTests
    {
        [Test]
        public void Should_throw_friendly_exception_when_IMutateIncomingTransportMessages_MutateIncoming_returns_null()
        {
            var behavior = new MutateIncomingTransportMessageBehavior();

            var incomingMessage = new IncomingMessage("messageId", new Dictionary<string, string>(), new MemoryStream());

            var context = new IncomingPhysicalMessageContext(incomingMessage, null);

            var builder = new FuncBuilder();

            builder.Register<IMutateIncomingTransportMessages>(() => new MutateIncomingTransportMessagesReturnsNull());

            context.Set<IBuilder>(builder);

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
