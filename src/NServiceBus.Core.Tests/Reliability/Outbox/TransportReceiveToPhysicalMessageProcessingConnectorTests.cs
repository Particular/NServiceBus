namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline.Contexts;
    using Transports;
    using NUnit.Framework;
    using TransportOperation = NServiceBus.Outbox.TransportOperation;

    [TestFixture]
    public class TransportReceiveToPhysicalMessageProcessingConnectorTests
    {

        [Test]
        public async Task Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            fakeOutbox.ExistingMessage = new OutboxMessage("id", new List<TransportOperation>());

            var context = new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), new RootContext(null));

            await Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        
        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
      
            behavior = new TransportReceiveToPhysicalMessageProcessingConnector(null,fakeOutbox);
        }

        async Task Invoke(TransportReceiveContext context)
        {
            await behavior.Invoke(context, c => TaskEx.Completed).ConfigureAwait(false);
        }

        FakeOutboxStorage fakeOutbox;
        TransportReceiveToPhysicalMessageProcessingConnector behavior;
    }
}