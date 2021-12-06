namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System;
    using System.IO;
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    [TestFixture]
    public class Using_Infer_Type_With_Non_Nested_Class
    {
        const string XmlWithBaseType = @"<?xml version=""1.0"" ?>
                <Messages
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                    xmlns=""http://tempuri.net/NServiceBus.Core.Tests.Serializers.XML""
                    xmlns:baseType=""NServiceBus.Core.Tests.Serializers.XML.IMyBusMessage"">
                    <FirstMessage></FirstMessage>
                    <SecondMessage></SecondMessage>
                    <SecondMessage></SecondMessage>
                    <SecondMessage></SecondMessage>
                </Messages>
                ";

        [Test]
        public void Should_deserialize_when_message_type_pre_discovered()
        {
            var serializer = SerializerFactory.Create(typeof(IMyBusMessage), typeof(FirstMessage), typeof(SecondMessage));
            var messageDeserialized = serializer.Deserialize(StringToByteArray(XmlWithBaseType));
            Assert.IsInstanceOf<FirstMessage>(messageDeserialized[0]);
            Assert.IsInstanceOf<SecondMessage>(messageDeserialized[1]);
            Assert.IsInstanceOf<SecondMessage>(messageDeserialized[2]);
            Assert.IsInstanceOf<SecondMessage>(messageDeserialized[3]);
        }

        [Test]
        public void Should_throw_exception_when_message_type_not_pre_discovered()
        {

            var serializer = SerializerFactory.Create();
            var exception = Assert.Throws<Exception>(() => serializer.Deserialize(StringToByteArray(XmlWithBaseType)));

            Assert.True(exception.Message.StartsWith("Could not determine type for node:"));
        }

        static byte[] StringToByteArray(string input)
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream))
            {
                streamWriter.Write(input);
                streamWriter.Flush();
                stream.Position = 0;

                return stream.ToArray();
            }
        }
    }

    public interface IMyBusMessage : IMessage
    {
    }

    public class FirstMessage : IMyBusMessage
    {
    }

    public class SecondMessage : IMyBusMessage
    {
    }
}