namespace NServiceBus.Core.Tests.Transports.Learning
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    public class LearningTransportDispatcherTests
    {
        [Test]
        public async Task Should_throw_for_size_above_threshold()
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "payload-to-big");
            var dispatcher = new LearningTransportDispatcher(path, 64);
            var messageAtThreshold = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[64 * 1024- emptyHeaderSize]);
            var messageAboveThreshold = new OutgoingMessage("id", new Dictionary<string, string>(), new byte[64 * 1024 - emptyHeaderSize + 1]);


            await dispatcher.Dispatch(new TransportOperations(new TransportOperation(messageAtThreshold, new UnicastAddressTag("my-destination"))), new TransportTransaction(), new ContextBag());


            var ex = Assert.ThrowsAsync<Exception>(async () => await dispatcher.Dispatch(new TransportOperations(new TransportOperation(messageAboveThreshold, new UnicastAddressTag("my-destination"))), new TransportTransaction(), new ContextBag()));

            StringAssert.Contains("Message body including headers", ex.Message);
        }

        const int emptyHeaderSize = 3;
    }
}