namespace NServiceBus.Transports.ActiveMQ.Tests.Encoders
{
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.Encoders;

    [TestFixture]
    public class ByteMessageEncoderTest
    {
        private ByteMessageEncoder testee;

        private Mock<ISession> session;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<ISession> { DefaultValue = DefaultValue.Mock };

            this.SetupMessageCreation();

            this.testee = new ByteMessageEncoder();
        }

        [Test]
        [TestCase(ContentTypes.Bson)]
        [TestCase(ContentTypes.Binary)]
        public void Encode_WhenBinaryContentTypeWithoutBody_ReturnEmptyBinaryMessage(string contentType)
        {
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsInstanceOf<IBytesMessage>(message);
            Assert.IsEmpty(((IBytesMessage)message).Content);
        }

        [Test]
        [TestCase(ContentTypes.Bson)]
        [TestCase(ContentTypes.Binary)]
        public void Encode_WhenBinaryContentTypeWithBody_ReturnFilledBinaryMessage(string contentType)
        {
            byte[] content = new byte[] { 2 };

            var transportMessage = new TransportMessage { Body = content };
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsInstanceOf<IBytesMessage>(message);
            Assert.AreEqual(content, ((IBytesMessage)message).Content);
        }

        [Test]
        [TestCase(ContentTypes.Xml)]
        [TestCase(ContentTypes.Json)]
        public void Encode_WhenNonBinaryContentType_ReturnNull(string contentType)
        {
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsNull(message);
        }

        private void SetupMessageCreation()
        {
            byte[] content = null;
            this.session.Setup(s => s.CreateBytesMessage(It.IsAny<byte[]>()))
                .Callback<byte[]>(c => content = c)
                .Returns(() => Mock.Of<IBytesMessage>(m => m.Content == content));
        }
    }
}