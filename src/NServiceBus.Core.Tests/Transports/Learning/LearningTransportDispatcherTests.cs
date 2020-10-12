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
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "payload-too-big");
            var dispatcher = new LearningTransportDispatcher(path, 64);
            var headers = new Dictionary<string, string> { { Headers.EnclosedMessageTypes, "TestMessage" } };
            var messageAtThreshold = new OutgoingMessage("id", headers, new byte[MessageSizeLimit]);
            var messageAboveThreshold = new OutgoingMessage("id", headers, new byte[MessageSizeLimit + 1]);

            await dispatcher.Dispatch(new TransportOperations(new TransportOperation(messageAtThreshold, new UnicastAddressTag("my-destination"), new Dictionary<string, string>())), new TransportTransaction());
            var ex = Assert.ThrowsAsync<Exception>(async () => await dispatcher.Dispatch(new TransportOperations(new TransportOperation(messageAboveThreshold, new UnicastAddressTag("my-destination"), new Dictionary<string, string>())), new TransportTransaction()));

            StringAssert.Contains("The total size of the 'TestMessage' message", ex.Message);
        }

        const int MessageSizeLimit = (64 * 1024) - headerSize;
        const int headerSize = 57;
    }
}