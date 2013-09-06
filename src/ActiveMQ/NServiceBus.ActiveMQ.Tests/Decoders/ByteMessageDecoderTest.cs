namespace NServiceBus.Transports.ActiveMQ.Tests.Decoders
{
    using ActiveMQ.Decoders;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ByteMessageDecoderTest
    {
        private ByteMessageDecoder testee;

        [SetUp]
        public void SetUp()
        {
            testee = new ByteMessageDecoder();
        }

        [Test]
        public void Decode_WhenByteMessage_ThenTrue()
        {
            var transportMessage = new TransportMessage();

            var result = testee.Decode(transportMessage, Mock.Of<IBytesMessage>());

            Assert.True(result);
        }

        [Test]
        public void Decode_WhenByteMessageWithContent_ThenAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            var bytesMessage = Mock.Of<IBytesMessage>(m => m.Content == new byte[] { 1 });

            testee.Decode(transportMessage, bytesMessage);

            Assert.NotNull(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenByteMessageWithNoContent_ThenNotAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            var bytesMessage = Mock.Of<IBytesMessage>();
            bytesMessage.Content = null;

            testee.Decode(transportMessage, bytesMessage);

            Assert.Null(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenNotByteMessage_ThenFalse()
        {
            var transportMessage = new TransportMessage();

            var result = testee.Decode(transportMessage, Mock.Of<ITextMessage>());

            Assert.False(result);
        }
    }
}