namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using TransportOperation = NServiceBus.Outbox.TransportOperation;

    [TestFixture]
    public class TransportReceiveToPhysicalMessageProcessingConnectorTests
    {

        [Test]
        public async Task Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            fakeOutbox.ExistingMessage = new OutboxMessage("id", new List<TransportOperation>());

            var context = new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null);

            await Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public void Should_throw_if_user_requested_abort()
        {
            var context = new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null);
            
            Assert.Throws<MessageProcessingAbortedException>(async () => await Invoke(context,true));
            Assert.False(fakeOutbox.WasDispatched);
        }

        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
      
            behavior = new TransportReceiveToPhysicalMessageProcessingConnector(null,fakeOutbox);
        }

        async Task Invoke(TransportReceiveContext context, bool shouldAbort = false)
        {
            await behavior.Invoke(context, c =>
            {
                c.AbortReceiveOperation = shouldAbort;

                return TaskEx.Completed;
            }).ConfigureAwait(false);
        }

        FakeOutboxStorage fakeOutbox;
        TransportReceiveToPhysicalMessageProcessingConnector behavior;
    }
}