namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System;
    using NServiceBus.Serializers.XML;
    using NUnit.Framework;

    [TestFixture]
    public class Using_Nested_Types
    {
        [Test]
        public void Should_throw_an_exception()
        {
            var serializer = new XmlMessageSerializer(null);
            var exception = Assert.Throws<Exception>(()=> serializer.Initialize(new[] {typeof(IMyBusMessage), typeof(M1), typeof(M2)}));
            Assert.AreEqual("Nested types are not supported by the XmlMessageSerializer.", exception.Message);
        }

        public interface IMyBusMessage : IMessage
        {
        }

        public class M1 : IMyBusMessage
        {
        }

        public class M2 : IMyBusMessage
        {
        }
    }
}
