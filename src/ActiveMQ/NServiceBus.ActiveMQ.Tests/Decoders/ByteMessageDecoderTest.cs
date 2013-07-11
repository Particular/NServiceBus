namespace NServiceBus.Transports.ActiveMQ.Tests.Decoders
{
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.Decoders;

    [TestFixture]
    public class ByteMessageDecoderTest
    {
        private ByteMessageDecoder testee;

        [SetUp]
        public void SetUp()
        {
            this.testee = new ByteMessageDecoder();
        }

        [Test]
        public void Decode_WhenByteMessage_ThenTrue()
        {
            var transportMessage = new TransportMessage();

            var result = this.testee.Decode(transportMessage, Mock.Of<IBytesMessage>());

            Assert.True(result);
        }

        [Test]
        public void Decode_WhenByteMessageWithContent_ThenAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            var bytesMessage = Mock.Of<IBytesMessage>(m => m.Content == new byte[] { 1 });

            this.testee.Decode(transportMessage, bytesMessage);

            Assert.NotNull(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenByteMessageWithNoContent_ThenNotAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            var bytesMessage = Mock.Of<IBytesMessage>();
            bytesMessage.Content = null;

            this.testee.Decode(transportMessage, bytesMessage);

            Assert.Null(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenNotByteMessage_ThenFalse()
        {
            var transportMessage = new TransportMessage();

            var result = this.testee.Decode(transportMessage, Mock.Of<ITextMessage>());

            Assert.False(result);
        }
    }
}