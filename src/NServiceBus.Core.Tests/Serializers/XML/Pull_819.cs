namespace NServiceBus.Serializers.XML.Test
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using NUnit.Framework;

    [TestFixture]
    public class Pull_819
    {
        [Test]
        public void Should_check_for_ignore_attribute_before_checking_type()
        {
            var result = ExecuteSerializer.ForMessage<MessageWithXmlIgnore>(m3 =>
            {
                m3.FirstName = "John";
                m3.LastName = "Smith";
#pragma warning disable DE0006
                m3.List = new ArrayList();
#pragma warning restore DE0006
                m3.GenericList = new List<string>();
            });

            Assert.AreEqual("John", result.FirstName);
        }

        public class MessageWithXmlIgnore : MessageWithXmlIgnoreBase
        {
            [XmlIgnore]
            public IList List { get; set; }

            public string FirstName { get; set; }
        }

        public class MessageWithXmlIgnoreBase : IMessage
        {
            [XmlIgnore]
            public List<string> GenericList { get; set; }

            public string LastName { get; set; }
        }
    }
}
