namespace NServiceBus.Transport.ActiveMQ
{
    using System;

    using Apache.NMS;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageEncoderPipelineTest
    {
        private Mock<IActiveMqMessageEncoder> firstDecoder;

        private Mock<IActiveMqMessageEncoder> secondDecoder;

        private ActiveMqMessageEncoderPipeline testee;

        [SetUp]
        public void SetUp()
        {
            this.firstDecoder = new Mock<IActiveMqMessageEncoder>();
            this.secondDecoder = new Mock<IActiveMqMessageEncoder>();

            this.testee =
                new ActiveMqMessageEncoderPipeline(new[] { this.firstDecoder.Object, this.secondDecoder.Object });
        }

        [Test]
        public void Encode_FirstDecoderReturnsDecodedMessage()
        {
            var expectedMessage = Mock.Of<IMessage>();
            this.firstDecoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(expectedMessage);

            IMessage message = this.testee.Encode(new TransportMessage(), Mock.Of<ISession>());

            this.secondDecoder.Verify(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>()), Times.Never());
            Assert.AreSame(expectedMessage, message);
        }

        [Test]
        public void Encode_WhenAllDecoderCannotDecode_ThenThrow()
        {
            this.firstDecoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(default(IMessage));
            this.secondDecoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(default(IMessage));

            Assert.Throws<InvalidOperationException>(() => this.testee.Encode(new TransportMessage(), Mock.Of<ISession>()));
        }
    }
}