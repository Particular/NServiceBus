namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class ActiveMqMessageDecoderPipelineTest
    {
        private Mock<IActiveMqMessageDecoder> firstDecoder;

        private Mock<IActiveMqMessageDecoder> secondDecoder;

        private ActiveMqMessageDecoderPipeline testee;

        [SetUp]
        public void SetUp()
        {
            this.firstDecoder = new Mock<IActiveMqMessageDecoder>();
            this.secondDecoder = new Mock<IActiveMqMessageDecoder>();

            this.testee =
                new ActiveMqMessageDecoderPipeline(new[] { this.firstDecoder.Object, this.secondDecoder.Object });
        }

        [Test]
        public void Decode_FirstDecoderReturnsDecodedMessage()
        {
            this.firstDecoder.Setup(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>())).Returns(true);

            this.testee.Decode(new TransportMessage(), Mock.Of<IMessage>());

            this.secondDecoder.Verify(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>()), Times.Never());
        }

        [Test]
        public void Decode_WhenAllDecoderCannotDecode_ThenThrow()
        {
            this.firstDecoder.Setup(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>())).Returns(false);
            this.secondDecoder.Setup(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>())).Returns(false);

            Assert.Throws<InvalidOperationException>(() => this.testee.Decode(new TransportMessage(), Mock.Of<IMessage>()));
        }
    }
}