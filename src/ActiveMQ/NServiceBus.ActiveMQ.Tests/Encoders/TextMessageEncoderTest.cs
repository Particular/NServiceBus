namespace NServiceBus.Transport.ActiveMQ.Encoders
{
    using Apache.NMS;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class TextMessageEncoderTest
    {
        private TextMessageEncoder testee;

        private Mock<ISession> session;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<ISession> { DefaultValue = DefaultValue.Mock };

            this.testee = new TextMessageEncoder();
        }

        [Test]
        [TestCase(ContentTypes.Json)]
        [TestCase(ContentTypes.Xml)]
        public void Encode_WhenTextContentType_ReturnTextMessage(string contentType)
        {
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsInstanceOf<ITextMessage>(message);
        }

        [Test]
        [TestCase(ContentTypes.Bson)]
        [TestCase(ContentTypes.Binary)]
        public void Encode_WhenNonTextContentType_ReturnNull(string contentType)
        {
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsNull(message);
        }
    }
}