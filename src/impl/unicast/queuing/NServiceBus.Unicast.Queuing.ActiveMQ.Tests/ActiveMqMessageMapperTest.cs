namespace NServiceBus.Unicast.Queuing.ActiveMQ.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Apache.NMS;
    using Apache.NMS.Util;

    using FluentAssertions;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageMapperTest
    {
        private ActiveMqMessageMapper testee;

        private Mock<INetTxSession> session;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<INetTxSession>();

            this.testee = new ActiveMqMessageMapper();
        }

        [Test]
        public void CreateJmsMessage_ShouldUseTextMessage()
        {
            this.SetupMessageCreation();

            const string ExpectedMessageBody = "Yehaa!";

            TransportMessage transportMessage = this.CreateTransportMessage(ExpectedMessageBody);

            var result = this.testee.CreateJmsMessage(transportMessage, this.session.Object) as ITextMessage;

            result.Should().NotBeNull();
            result.Text.Should().Be(ExpectedMessageBody);
        }

        [Test]
        public void CreateTransportMessage_ShouldUseTextMessage()
        {
            const string ExpectedMessageBody = "Yehaa!";
            var primitiveMap = new PrimitiveMap();
            primitiveMap[ActiveMqMessageMapper.MessageIntentKey] = MessageIntentEnum.Send;

            var message = new Mock<ITextMessage>();
            message.Setup(x => x.Text).Returns(ExpectedMessageBody);
            message.Setup(x => x.Properties).Returns(primitiveMap);
            message.Setup(x => x.NMSReplyTo).Returns(Mock.Of<IDestination>);

            TransportMessage expectedTransportMessage = this.CreateTransportMessage(ExpectedMessageBody);

            var result = this.testee.CreateTransportMessage(message.Object);

            result.Body.Should().BeEquivalentTo(expectedTransportMessage.Body);
        }

        private void SetupMessageCreation()
        {
            string body = null;
            this.session.Setup(s => s.CreateTextMessage(It.IsAny<string>()))
                .Callback<string>(b => body = b)
                .Returns(() => Mock.Of<ITextMessage>(m => m.Text == body && m.Properties == new PrimitiveMap()));
        }

        private TransportMessage CreateTransportMessage(string body)
        {
            return this.CreateTransportMessage(Encoding.UTF8.GetBytes(body));
        }

        private TransportMessage CreateTransportMessage(byte[] body)
        {
            return new TransportMessage
                {
                    Body = body,
                    Headers = new Dictionary<string, string> { { Headers.EnclosedMessageTypes, "FancyHeader" }, },
                    Recoverable = true,
                    TimeToBeReceived = TimeSpan.FromSeconds(2),
                    ReplyToAddress = new Address("someAddress", "localhorst")
                };
        }
    }
}