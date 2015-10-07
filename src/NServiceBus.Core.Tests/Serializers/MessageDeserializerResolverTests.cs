namespace NServiceBus.Core.Tests.Serializers
{
    using System;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Serialization;
    using NServiceBus.Serializers;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Serializers.XML;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class MessageDeserializerResolverTests
    {
        MessageDeserializerResolver resolver;

        [SetUp]
        public void Setup()
        {
            var mapper = new MessageMapper();
            var xml = new XmlMessageSerializer(mapper, new Conventions());
            var json = new JsonMessageSerializer(mapper);
            resolver = new MessageDeserializerResolver(new IMessageSerializer[] { xml, json}, xml.GetType());
        }

        [TestCase(ContentTypes.Xml, typeof(XmlMessageSerializer))]
        [TestCase(ContentTypes.Json, typeof(JsonMessageSerializer))]
        public void RetrievesSerializerByContentType(string contentType, Type expected)
        {
            var serializer = resolver.Resolve(contentType);
            Assert.IsInstanceOf(expected, serializer);
        }

        [Test]
        public void UnknownContentTypeFallsBackToXmlSerialization()
        {
            var serializer = resolver.Resolve("unknown/unsupported");
            Assert.IsInstanceOf<XmlMessageSerializer>(serializer);
        }
    }
}