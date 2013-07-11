namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class ActiveMqMessageEncoderPipelineTest
    {
        private Mock<IActiveMqMessageEncoder> firstEncoder;

        private Mock<IActiveMqMessageEncoder> secondEncoder;

        private ActiveMqMessageEncoderPipeline testee;

        [SetUp]
        public void SetUp()
        {
            this.firstEncoder = new Mock<IActiveMqMessageEncoder>();
            this.secondEncoder = new Mock<IActiveMqMessageEncoder>();

            this.testee =
                new ActiveMqMessageEncoderPipeline(new[] { this.firstEncoder.Object, this.secondEncoder.Object });
        }

        [Test]
        public void Encode_FirstEncoderReturnsEncodedMessage()
        {
            var expectedMessage = Mock.Of<IMessage>();
            this.firstEncoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(expectedMessage);

            IMessage message = this.testee.Encode(new TransportMessage(), Mock.Of<ISession>());

            this.secondEncoder.Verify(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>()), Times.Never());
            Assert.AreSame(expectedMessage, message);
        }

        [Test]
        public void Encode_WhenAllEncoderCannotEncode_ThenThrow()
        {
            this.firstEncoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(default(IMessage));
            this.secondEncoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(default(IMessage));

            Assert.Throws<InvalidOperationException>(() => this.testee.Encode(new TransportMessage(), Mock.Of<ISession>()));
        }
    }
}