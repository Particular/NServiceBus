namespace NServiceBus.Transports.ActiveMQ.Tests.Decoders
{
    using Apache.NMS;
    using Apache.NMS.Util;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ.Decoders;

    [TestFixture]
    public class ControlMessageDecoderTest
    {
        private ControlMessageDecoder testee;

        [SetUp]
        public void SetUp()
         {
             this.testee = new ControlMessageDecoder();
         }

        [Test]
        public void Decode_WhenControlMessage_ThenTrue()
        {
            var primitiveMap = new PrimitiveMap();
            primitiveMap[Headers.ControlMessageHeader] = null;

            var message = Mock.Of<IBytesMessage>(x => x.Properties == primitiveMap);

            var result = this.testee.Decode(new TransportMessage(), message);

            Assert.IsTrue(result);
        }

        [Test]
        public void Decode_WhenControlMessageWithContent_ThenAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };

            var primitiveMap = new PrimitiveMap();
            primitiveMap[Headers.ControlMessageHeader] = null;

            var bytesMessage = Mock.Of<IBytesMessage>(m => m.Properties == primitiveMap && m.Content == new byte[] { 1 });

            this.testee.Decode(transportMessage, bytesMessage);

            Assert.NotNull(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenControlMessageWithNoContent_ThenNotAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            transportMessage.Headers.Add(Headers.ControlMessageHeader, null);

            var bytesMessage = Mock.Of<IBytesMessage>();
            bytesMessage.Content = null;

            this.testee.Decode(transportMessage, bytesMessage);

            Assert.Null(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenNotControlMessage_ThenFalse()
        {
            var message = Mock.Of<IMessage>(x => x.Properties == new PrimitiveMap());

            var result = this.testee.Decode(new TransportMessage(), message);

            Assert.IsFalse(result);
        }
    }
}