namespace NServiceBus.Transports.ActiveMQ.Tests.Decoders
{
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.Decoders;

    [TestFixture]
    public class TestMessageDecoderTest
    {
        private TextMessageDecoder testee;

        [SetUp]
        public void SetUp()
        {
            this.testee = new TextMessageDecoder();
        }

        [Test]
        public void Decode_WhenTextMessage_ThenTrue()
        {
            var transportMessage = new TransportMessage();

            var result = this.testee.Decode(transportMessage, Mock.Of<ITextMessage>());

            Assert.True(result);
        }

        [Test]
        public void Decode_WhenTextMessageWithText_ThenAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            var textMessage = Mock.Of<ITextMessage>(m => m.Text == "SomeContent");

            this.testee.Decode(transportMessage, textMessage);

            Assert.NotNull(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenTextMessageWithNoText_ThenNotAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            var textMessage = Mock.Of<ITextMessage>();

            this.testee.Decode(transportMessage, textMessage);

            Assert.Null(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenNotTextMessage_ThenFalse()
        {
            var transportMessage = new TransportMessage();

            var result = this.testee.Decode(transportMessage, Mock.Of<IBytesMessage>());

            Assert.False(result);
        }
    }
}