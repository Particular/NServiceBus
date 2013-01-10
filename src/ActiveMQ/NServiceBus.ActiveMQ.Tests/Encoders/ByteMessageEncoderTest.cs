namespace NServiceBus.Transport.ActiveMQ.Encoders
{
    using System;

    using Apache.NMS;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ByteMessageEncoderTest
    {
        private ByteMessageEncoder testee;

        private Mock<ISession> session;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<ISession> { DefaultValue = DefaultValue.Mock };

            this.testee = new ByteMessageEncoder();
        }

        [Test]
        [TestCase(ContentTypes.Bson)]
        [TestCase(ContentTypes.Binary)]
        public void Encode_WhenBinaryContentType_ReturnBinaryMessage(string contentType)
        {
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsInstanceOf<IBytesMessage>(message);
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
    }
}