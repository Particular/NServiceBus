namespace NServiceBus.Transports.ActiveMQ.Tests.Decoders
{
    using ActiveMQ.Decoders;
    using Apache.NMS;
    using Apache.NMS.Util;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class ControlMessageDecoderTest
    {
        private ControlMessageDecoder testee;

        [SetUp]
        public void SetUp()
         {
             testee = new ControlMessageDecoder();
         }

        [Test]
        public void Decode_WhenControlMessage_ThenTrue()
        {
            var primitiveMap = new PrimitiveMap();
            primitiveMap[Headers.ControlMessageHeader] = null;

            var message = Mock.Of<IBytesMessage>(x => x.Properties == primitiveMap);

            var result = testee.Decode(new TransportMessage(), message);

            Assert.IsTrue(result);
        }

        [Test]
        public void Decode_WhenControlMessageWithContent_ThenAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };

            var primitiveMap = new PrimitiveMap();
            primitiveMap[Headers.ControlMessageHeader] = null;

            var bytesMessage = Mock.Of<IBytesMessage>(m => m.Properties == primitiveMap && m.Content == new byte[] { 1 });

            testee.Decode(transportMessage, bytesMessage);

            Assert.NotNull(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenControlMessageWithNoContent_ThenNotAssignBody()
        {
            var transportMessage = new TransportMessage { Body = null };
            transportMessage.Headers.Add(Headers.ControlMessageHeader, null);

            var bytesMessage = Mock.Of<IBytesMessage>();
            bytesMessage.Content = null;

            testee.Decode(transportMessage, bytesMessage);

            Assert.Null(transportMessage.Body);
        }

        [Test]
        public void Decode_WhenNotControlMessage_ThenFalse()
        {
            var message = Mock.Of<IMessage>(x => x.Properties == new PrimitiveMap());

            var result = testee.Decode(new TransportMessage(), message);

            Assert.IsFalse(result);
        }
    }
}