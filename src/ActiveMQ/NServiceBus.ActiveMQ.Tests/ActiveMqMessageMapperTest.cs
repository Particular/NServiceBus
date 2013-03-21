﻿namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using System.Collections.Generic;

    using Apache.NMS;
    using Apache.NMS.Util;

    using FluentAssertions;

    using Moq;

    using NServiceBus.Transports.ActiveMQ;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqMessageMapperTest
    {
        private ActiveMqMessageMapper testee;
        private Mock<ISession> session;
        private Mock<IMessageTypeInterpreter> messageTypeInterpreter;

        private Mock<IActiveMqMessageDecoderPipeline> decoderPipeline;

        private Mock<IActiveMqMessageEncoderPipeline> encoderPipeline;

        [SetUp]
        public void SetUp()
        {
            this.session = new Mock<ISession>();
            this.messageTypeInterpreter = new Mock<IMessageTypeInterpreter>();
            this.encoderPipeline = new Mock<IActiveMqMessageEncoderPipeline>();
            this.decoderPipeline = new Mock<IActiveMqMessageDecoderPipeline>();

            this.testee = new ActiveMqMessageMapper(this.messageTypeInterpreter.Object, this.encoderPipeline.Object, this.decoderPipeline.Object);
        }

        [Test]
        public void CreateJmsMessage_ShouldEncodeMessage()
        {
            this.SetupMessageCreation();

            TransportMessage transportMessage = this.CreateTransportMessage();

            this.testee.CreateJmsMessage(transportMessage, this.session.Object);

            this.encoderPipeline.Verify(e => e.Encode(transportMessage, this.session.Object));
        }

        [Test]
        public void CreateJmsMessage_ShouldSetNMSTypeToNamespaceAndTypeOnlyForBetterInteroperability()
        {
            this.SetupMessageCreation();

            TransportMessage transportMessage = this.CreateTransportMessage();
            transportMessage.Headers[Headers.EnclosedMessageTypes] = typeof(string).AssemblyQualifiedName;

            var message = this.testee.CreateJmsMessage(transportMessage, this.session.Object);

            message.NMSType.Should().Be("System.String");
        }

        [Test]
        public void CreateJmsMessage_WhenMultipleEnclosedTypes_ShouldSetNMSTypeToNamespaceAndTypeOnlyForBetterInteroperability()
        {
            this.SetupMessageCreation();

            TransportMessage transportMessage = this.CreateTransportMessage();
            transportMessage.Headers[Headers.EnclosedMessageTypes] = string.Format("{0};{1}", typeof(string).AssemblyQualifiedName, typeof(int).AssemblyQualifiedName);

            var message = this.testee.CreateJmsMessage(transportMessage, this.session.Object);

            message.NMSType.Should().Be("System.String");
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
        public void CreateTransportMessage_ShouldDecodeMessage()
        {
            var message = CreateTextMessage("SomeContent");

            this.testee.CreateTransportMessage(message);

            this.decoderPipeline.Verify(d => d.Decode(It.IsAny<TransportMessage>(), message));
        }

        [Test]
        public void CreateTransportMessage_IfNServiceBusVersioIsDefined_ShouldAssignNServiceBusVersion()
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
        public void CreateTransportMessage_IfEnclosedMessageTypesIsDefined_ShouldAssignIt()
        {
            string EnclosedMessageTypes = typeof(string).AssemblyQualifiedName;

            var message = CreateTextMessage(string.Empty);
            message.Properties[Headers.EnclosedMessageTypes] = EnclosedMessageTypes;

            var result = this.testee.CreateTransportMessage(message);

            result.Headers[Headers.EnclosedMessageTypes].Should().Be(EnclosedMessageTypes);
        }

        [Test]
        public void CreateTransportMessage_IfEnclosedMessageTypesIsNotDefined_ShouldAssignInterpretedTypeFromJmsMessage()
        {
            string ExpectedEnclosedMessageTypes = typeof(string).AssemblyQualifiedName;
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

        [Test]
        public void CreateTransportMessage_WhenHeaderWith_DOT_ThenConvertedtoDot()
        {
            const string Value = "Value";
            var message = CreateTextMessage(string.Empty);
            message.Properties["NSB_DOT_Feature"] = Value;

            var result = this.testee.CreateTransportMessage(message);

            result.Headers["NSB.Feature"].Should().Be(Value);
        }

        [Test]
        public void CreateTransportMessage_WhenHeaderWith_HYPHEN_ThenConvertedtoHyphen()
        {
            const string Value = "Value";
            var message = CreateTextMessage(string.Empty);
            message.Properties["NSB_HYPHEN_Feature"] = Value;

            var result = this.testee.CreateTransportMessage(message);

            result.Headers["NSB-Feature"].Should().Be(Value);
        }

        [Test]
        public void ConvertMessageHeaderKeyFromActiveMQ_Converts_DOT_toDot()
        {
            var header = ActiveMqMessageMapper.ConvertMessageHeaderKeyFromActiveMQ("NSB_DOT_Feature");

            header.Should().Be("NSB.Feature");
        }

        [Test]
        public void ConvertMessageHeaderKeyFromActiveMQ_Converts_HYPHEN_toHyphen()
        {
            var header = ActiveMqMessageMapper.ConvertMessageHeaderKeyFromActiveMQ("NSB_HYPHEN_Feature");

            header.Should().Be("NSB-Feature");
        }

        [Test]
        public void ConvertMessageHeaderKeyToActiveMQ_ConvertsDot_to_DOT_()
        {
            var header = ActiveMqMessageMapper.ConvertMessageHeaderKeyToActiveMQ("NSB.Feature");

            header.Should().Be("NSB_DOT_Feature");
        }

        [Test]
        public void ConvertMessageHeaderKeyToActiveMQ_Converts_Hyphen_to_HYPHEN_()
        {
            var header = ActiveMqMessageMapper.ConvertMessageHeaderKeyToActiveMQ("NSB-Feature");

            header.Should().Be("NSB_HYPHEN_Feature");
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
            var message = new Mock<IMessage>();
            message.SetupAllProperties();
            message.Setup(m => m.Properties).Returns(new PrimitiveMap());

            this.encoderPipeline.Setup(e => e.Encode(It.IsAny<TransportMessage>(), It.IsAny<ISession>()))
                .Returns(message.Object);
        }

        private TransportMessage CreateTransportMessage()
        {
            return new TransportMessage
                {
                    Headers = new Dictionary<string, string> { { Headers.EnclosedMessageTypes, "FancyHeader" }, },
                    Recoverable = true,
                    TimeToBeReceived = TimeSpan.FromSeconds(2),
                    ReplyToAddress = new Address("someAddress", "localhorst")
                };
        }
    }
}