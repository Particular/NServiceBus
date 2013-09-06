namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageDecoderPipelineTest
    {
        private Mock<IActiveMqMessageDecoder> firstDecoder;

        private Mock<IActiveMqMessageDecoder> secondDecoder;

        private ActiveMqMessageDecoderPipeline testee;

        [SetUp]
        public void SetUp()
        {
            firstDecoder = new Mock<IActiveMqMessageDecoder>();
            secondDecoder = new Mock<IActiveMqMessageDecoder>();

            testee =
                new ActiveMqMessageDecoderPipeline(new[] { firstDecoder.Object, secondDecoder.Object });
        }

        [Test]
        public void Decode_FirstDecoderReturnsDecodedMessage()
        {
            firstDecoder.Setup(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>())).Returns(true);

            testee.Decode(new TransportMessage(), Mock.Of<IMessage>());

            secondDecoder.Verify(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>()), Times.Never());
        }

        [Test]
        public void Decode_WhenAllDecoderCannotDecode_ThenThrow()
        {
            firstDecoder.Setup(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>())).Returns(false);
            secondDecoder.Setup(d => d.Decode(It.IsAny<TransportMessage>(), It.IsAny<IMessage>())).Returns(false);

            Assert.Throws<InvalidOperationException>(() => testee.Decode(new TransportMessage(), Mock.Of<IMessage>()));
        }
    }
}