namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageEncoderPipelineTest
    {
        private Mock<IActiveMqMessageEncoder> firstEncoder;

        private Mock<IActiveMqMessageEncoder> secondEncoder;

        private ActiveMqMessageEncoderPipeline testee;

        [SetUp]
        public void SetUp()
        {
            firstEncoder = new Mock<IActiveMqMessageEncoder>();
            secondEncoder = new Mock<IActiveMqMessageEncoder>();

            testee =
                new ActiveMqMessageEncoderPipeline(new[] { firstEncoder.Object, secondEncoder.Object });
        }

        [Test]
        public void Encode_FirstEncoderReturnsEncodedMessage()
        {
            var expectedMessage = Mock.Of<IMessage>();
            firstEncoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(expectedMessage);

            var message = testee.Encode(new TransportMessage(), Mock.Of<ISession>());

            secondEncoder.Verify(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>()), Times.Never());
            Assert.AreSame(expectedMessage, message);
        }

        [Test]
        public void Encode_WhenAllEncoderCannotEncode_ThenThrow()
        {
            firstEncoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(default(IMessage));
            secondEncoder.Setup(d => d.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>())).Returns(default(IMessage));

            Assert.Throws<InvalidOperationException>(() => testee.Encode(new TransportMessage(), Mock.Of<ISession>()));
        }
    }
}