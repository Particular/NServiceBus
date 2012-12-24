namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Apache.NMS;
    using Apache.NMS.ActiveMQ.Commands;
    using Apache.NMS.Util;

    using FluentAssertions;

    using Moq;

    using NServiceBus.Unicast.Transport;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageMapperTest
    {
        private ActiveMqMessageMapper testee;
        private Mock<INetTxSession> session;
        private Mock<IMessageTypeInterpreter> messageTypeInterpreter;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<INetTxSession>();
            this.messageTypeInterpreter = new Mock<IMessageTypeInterpreter>();
            this.testee = new ActiveMqMessageMapper(this.messageTypeInterpreter.Object);
        }

        [Test]
        public void CreateJmsMessage_WhenControlMessage_ShouldUseEmptyMessage()
        {
            this.SetupMessageCreation();

            var controlMessage = ControlMessage.Create(Address.Local);

            var result = this.testee.CreateJmsMessage(controlMessage, this.session.Object) as ITextMessage;

            result.Should().NotBeNull();
            result.Text.Should().BeNull();
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
        public void CreateTransportMessage_WhenHeaderKeyNull_ShouldAddNullHeaderKey()
        {
            const string KeyWhichIsNull = "SomeKeyWhichIsNull";

            var message = CreateTextMessage(string.Empty);
            message.Properties[KeyWhichIsNull] = null;

            var result = this.testee.CreateTransportMessage(message);

            result.Headers[KeyWhichIsNull].Should().BeNull();
        }

        [Test]
        public void CreateTransportMessage_WhenControlMessage_ShouldUseNullBody()
        {
            var message = CreateTextMessage(default(string));
            message.Properties[Headers.ControlMessageHeader] = "true";

            var result = this.testee.CreateTransportMessage(message);

            result.Body.Should().BeNull();
        }

        [Test]
        public void CreateTransportMessage_ShouldUseTextMessage()
        {
            const string ExpectedMessageBody = "Yehaa!";
            var expectedTransportMessage = this.CreateTransportMessage(ExpectedMessageBody);

            var message = CreateTextMessage(ExpectedMessageBody);

            var result = this.testee.CreateTransportMessage(message);

            result.Body.Should().BeEquivalentTo(expectedTransportMessage.Body);
        }

        [Test]
        public void CreateTransportMessage_IfNServiceBusVersioIsDefined_ShouldAssignNSeriveBusVersion()
        {
            const string Version = "2.0.0.0";
            var message = CreateTextMessage(string.Empty);
            message.Properties[Headers.NServiceBusVersion] = Version;

            var result = this.testee.CreateTransportMessage(message);

            result.Headers[Headers.NServiceBusVersion].Should().Be(Version);
        }

        [Test]
        public void CreateTransportMessage_IfNServiceBusVersioIsNotDefined_ShouldAssignDefaultNServiceBusVersion()
        {
            const string Version = "4.0.0.0";
            var message = CreateTextMessage(string.Empty);

            var result = this.testee.CreateTransportMessage(message);

            result.Headers[Headers.NServiceBusVersion].Should().Be(Version);
        }

        [Test]
        public void CreateTransportMessage_IfMessageIntentIsDefined_ShouldAssignMessageIntent()
        {
            const MessageIntentEnum Intent = MessageIntentEnum.Subscribe;
            var message = CreateTextMessage(string.Empty);
            message.Properties[ActiveMqMessageMapper.MessageIntentKey] = Intent;

            var result = this.testee.CreateTransportMessage(message);

            result.MessageIntent.Should().Be(Intent);
        }

        [Test]
        public void CreateTransportMessage_ForPublicationMessage_IfMessageIntentIsNotDefined_ShouldAssignPublishToMessageIntent()
        {
            var message = CreateTextMessage(string.Empty);
            message.NMSDestination = new ActiveMQTopic("myTopic");

            var result = this.testee.CreateTransportMessage(message);

            result.MessageIntent.Should().Be(MessageIntentEnum.Publish);
        }

        [Test]
        public void CreateTransportMessage_ForSendMessage_IfMessageIntentIsNotDefined_ShouldAssignSendToMessageIntent()
        {
            var message = CreateTextMessage(string.Empty);
            message.NMSDestination = new ActiveMQQueue("myQueue");

            var result = this.testee.CreateTransportMessage(message);

            result.MessageIntent.Should().Be(MessageIntentEnum.Send);
        }

        [Test]
        public void CreateTransportMessage_IfEnclosedMessageTypesIsDefined_ShouldAssignIt()
        {
            const string EnclosedMessageTypes = "TheEnclosedMessageTypes";
            var message = CreateTextMessage(string.Empty);
            message.Properties[Headers.EnclosedMessageTypes] = EnclosedMessageTypes;

            var result = this.testee.CreateTransportMessage(message);

            result.Headers[Headers.EnclosedMessageTypes].Should().Be(EnclosedMessageTypes);
        }

        [Test]
        public void CreateTransportMessage_IfEnclosedMessageTypesIsNotDefined_ShouldAssignInterpretedTypeFromJmsMessage()
        {
            const string ExpectedEnclosedMessageTypes = "TheEnclosedMessageTypes";
            const string JmsMessageType = "JmsMessageType";
            var message = CreateTextMessage(string.Empty);
            message.NMSType = JmsMessageType;

            this.messageTypeInterpreter
                .Setup(i => i.GetAssemblyQualifiedName(JmsMessageType))
                .Returns(ExpectedEnclosedMessageTypes);

            var result = this.testee.CreateTransportMessage(message);

            result.Headers[Headers.EnclosedMessageTypes].Should().Be(ExpectedEnclosedMessageTypes);
        }

        [Test]
        public void CreateTransportMessage_IfEnclosedMessageTypesIsNotDefinedAndNoJmsType_ShouldNotAddEnclosedMessageTypes()
        {
            var message = CreateTextMessage(string.Empty);

            var result = this.testee.CreateTransportMessage(message);

            result.Headers.Should().NotContainKey(Headers.EnclosedMessageTypes);
        }
        
        [Test]
        public void CreateTransportMessage_ShouldAssignCorrelationId()
        {
            const string CorrelationId = "TheCorrelationId";
            var message = CreateTextMessage(string.Empty);
            message.NMSCorrelationID = CorrelationId;

            var result = this.testee.CreateTransportMessage(message);

            result.CorrelationId.Should().Be(CorrelationId);
        }


        [Test]
        public void CreateTransportMessage_WhenMessageHasErrorCodeKey_ShouldAssignReturnMessageErrorCodeHeader()
        {
            const string Error = "Error";
            var message = CreateTextMessage(string.Empty);
            message.Properties[ActiveMqMessageMapper.ErrorCodeKey] = Error;

            var result = this.testee.CreateTransportMessage(message);

            result.Headers[Headers.ReturnMessageErrorCodeHeader].Should().Be(Error);
            result.Headers[Headers.ControlMessageHeader].Should().Be("true");
        }
        
        private static ITextMessage CreateTextMessage(string body)
        {
            var message = new Mock<ITextMessage>();
            message.SetupAllProperties();

            message.Setup(m => m.Properties).Returns(new PrimitiveMap());
            message.Object.Text = body;

            return message.Object;
        }

        private void SetupMessageCreation()
        {
            string body = null;
            this.session.Setup(s => s.CreateTextMessage())
                .Returns(() => Mock.Of<ITextMessage>(m => m.Properties == new PrimitiveMap()));

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