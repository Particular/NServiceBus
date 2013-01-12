﻿namespace NServiceBus.Transport.ActiveMQ.Encoders
{
    using System.Text;

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

            this.SetupMessageCreation();

            this.testee = new TextMessageEncoder();
        }

        [Test]
        [TestCase(ContentTypes.Json)]
        [TestCase(ContentTypes.Xml)]
        public void Encode_WhenTextContentTypeWithoutBody_ReturnEmptyTextMessage(string contentType)
        {
            var transportMessage = new TransportMessage();
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsInstanceOf<ITextMessage>(message);
            Assert.IsNullOrEmpty(((ITextMessage)message).Text);
        }

        [Test]
        [TestCase(ContentTypes.Json)]
        [TestCase(ContentTypes.Xml)]
        public void Encode_WhenTextContentTypeWithBody_ReturnFilledTextMessage(string contentType)
        {
            const string ExpectedContent = "SomeContent";

            var transportMessage = new TransportMessage
                                       {
                                           Body = Encoding.UTF8.GetBytes(ExpectedContent),
                                       };
            transportMessage.Headers.Add(Headers.ContentType, contentType);

            IMessage message = this.testee.Encode(transportMessage, this.session.Object);

            Assert.IsInstanceOf<ITextMessage>(message);
            Assert.AreEqual(ExpectedContent, ((ITextMessage)message).Text);
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

        private void SetupMessageCreation()
        {
            string content = null;
            this.session.Setup(s => s.CreateTextMessage(It.IsAny<string>()))
                .Callback<string>(c => content = c)
                .Returns(() => Mock.Of<ITextMessage>(m => m.Text == content));
        }
    }
}