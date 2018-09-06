namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using NUnit.Framework;

    
    public class MessageWithNullable : IMessage
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmailAddress { get; set; }

        public DateTime? BirthDate { get; set; } //Nullable DateTime property
    }

    [TestFixture]
    public class SerializingNullableTypesTests
    {
        [Test]
        public void NullableTypesSerializeToXsiNilWhenNull()
        {
            var message = new MessageWithNullable
            {
                FirstName = "FirstName",
                LastName = "LastName",
                EmailAddress = "EmailAddress",
                BirthDate = null
            };

            using (var stream = new MemoryStream())
            {
                SerializerFactory.Create<MessageWithNullable>().Serialize(message, stream);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var xml = reader.ReadToEnd();

                var expected = XDocument.Parse(@"<?xml version=""1.0""?>
<MessageWithNullable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
   <FirstName>FirstName</FirstName>
   <LastName>LastName</LastName>
   <EmailAddress>EmailAddress</EmailAddress>
   <BirthDate xsi:nil=""true""></BirthDate>
</MessageWithNullable>
");
                var actual = XDocument.Parse(xml);

                Assert.AreEqual(expected.ToString(), actual.ToString());
            }
        }

        [Test]
        public void NullableTypeSerializeToValueWhenNotNull()
        {
            var message = new MessageWithNullable
            {
                FirstName = "FirstName",
                LastName = "LastName",
                EmailAddress = "EmailAddress",
                BirthDate = new DateTime(1950, 04, 25)
            };

            using (var stream = new MemoryStream())
            {
                SerializerFactory.Create<MessageWithNullable>().Serialize(message, stream);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                var xml = reader.ReadToEnd();

                var expected = XDocument.Parse(@"<?xml version=""1.0""?>
<MessageWithNullable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
   <FirstName>FirstName</FirstName>
   <LastName>LastName</LastName>
   <EmailAddress>EmailAddress</EmailAddress>
   <BirthDate>1950-04-25T00:00:00</BirthDate>
</MessageWithNullable>
");
                var actual = XDocument.Parse(xml);

                Assert.AreEqual(expected.ToString(), actual.ToString());
            }
        }

        [Test]
        public void CanDeserializeNilMessage()
        {
            var messageXml = @"<?xml version=""1.0""?>
<MessageWithNullable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
   <FirstName>FirstName</FirstName>
   <LastName>LastName</LastName>
   <EmailAddress>EmailAddress</EmailAddress>
   <BirthDate xsi:nil=""true""></BirthDate>
</MessageWithNullable>
";

            var data = Encoding.UTF8.GetBytes(messageXml);

            using (var stream = new MemoryStream(data))
            {
                var msgArray = SerializerFactory.Create<MessageWithNullable>().Deserialize(stream, new[] { typeof(MessageWithNullable) });
                var result = (MessageWithNullable)msgArray[0];

                Assert.AreEqual(null, result.BirthDate);
            }
        }

        [Test]
        public void CanDeserializeOriginalNullValueMessage()
        {
            var messageXml = @"<?xml version=""1.0""?>
<MessageWithNullable xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
   <FirstName>FirstName</FirstName>
   <LastName>LastName</LastName>
   <EmailAddress>EmailAddress</EmailAddress>
   <BirthDate>null</BirthDate>
</MessageWithNullable>
";

            var data = Encoding.UTF8.GetBytes(messageXml);

            using (var stream = new MemoryStream(data))
            {
                var msgArray = SerializerFactory.Create<MessageWithNullable>().Deserialize(stream, new[] { typeof(MessageWithNullable) });
                var result = (MessageWithNullable)msgArray[0];

                Assert.AreEqual(null, result.BirthDate);
            }
        }
    }
}