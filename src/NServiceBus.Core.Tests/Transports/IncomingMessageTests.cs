namespace NServiceBus.Core.Tests.Transports
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class IncomingMessageTests
    {
        [Test]
        public void Should_assign_transport_message_id_when_NServiceBus_message_id_header_is_missing()
        {
            var headers = new Dictionary<string, string>();
            var message = new IncomingMessage("nativeId", headers, new byte[]{});

            Assert.AreEqual("nativeId", message.NativeMessageId);
            Assert.AreEqual("nativeId", message.MessageId);
            Assert.AreEqual("nativeId", headers[Headers.MessageId]);
        }

        [Test]
        public void Should_retain_transport_message_id_when_NServiceBus_message_id_header_is_found()
        {
            var headers = new Dictionary<string, string>
            {
                { Headers.MessageId, "coreId" }
            };
            var message = new IncomingMessage("nativeId", headers, new byte[]{});

            Assert.AreEqual("nativeId", message.NativeMessageId);
            Assert.AreEqual("coreId", message.MessageId);
        }

        [Test]
        public void Should_assign_transport_message_id_when_NServiceBus_message_id_header_is_found_without_value()
        {
            var headers = new Dictionary<string, string>
            {
                { Headers.MessageId, "" }
            };
            var message = new IncomingMessage("nativeId", headers, new byte[]{});

            Assert.AreEqual("nativeId", message.NativeMessageId);
            Assert.AreEqual("nativeId", message.MessageId);
        }
    }
}