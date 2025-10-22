namespace NServiceBus.Core.Tests.Serializers;

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Serialization;

[TestFixture]
public class MessageDeserializerResolverTests
{
    [TestCase(ContentTypes.Xml)]
    [TestCase(ContentTypes.Json)]
    public void RetrievesSerializerByContentType(string contentType)
    {
        var expectedResolver = new MessageSerializerBag(new FakeSerializer(contentType), false);
        var resolver = new MessageDeserializerResolver(new MessageSerializerBag(new FakeSerializer("default"), false), new MessageSerializerBag[]
        {
            new MessageSerializerBag(new FakeSerializer("some/content/type"), false),
            expectedResolver,
            new MessageSerializerBag(new FakeSerializer("another/content/type"), false),
        });

        var headers = new Dictionary<string, string>
        {
            {Headers.ContentType, contentType}
        };
        var serializer = resolver.Resolve(headers);
        Assert.That(serializer, Is.SameAs(expectedResolver));
    }

    [Test]
    public void UnknownContentTypeFallsBackToDefaultSerialization()
    {
        var mainSerializer = new MessageSerializerBag(new FakeSerializer(ContentTypes.Xml), false);
        var resolver = new MessageDeserializerResolver(mainSerializer, new MessageSerializerBag[]
        {
            new MessageSerializerBag(new FakeSerializer(ContentTypes.Json), false)
        });

        var headers = new Dictionary<string, string>
        {
            {Headers.ContentType, "unknown/unsupported"}
        };
        var serializer = resolver.Resolve(headers);

        Assert.That(serializer, Is.SameAs(mainSerializer));
    }

    [Test]
    public void NoContentTypeFallsBackToDefaultSerialization()
    {
        var mainSerializer = new MessageSerializerBag(new FakeSerializer(ContentTypes.Xml), false);
        var resolver = new MessageDeserializerResolver(mainSerializer, new MessageSerializerBag[]
        {
            new MessageSerializerBag(new FakeSerializer(ContentTypes.Json), false)
        });

        var serializer = resolver.Resolve([]);

        Assert.That(serializer, Is.EqualTo(mainSerializer));
    }

    [TestCase(null)]
    [TestCase("")]
    public void EmptyContentTypeFallsBackToDefaultSerialization(string headerValue)
    {
        var mainSerializer = new MessageSerializerBag(new FakeSerializer(ContentTypes.Xml), false);
        var resolver = new MessageDeserializerResolver(mainSerializer, new MessageSerializerBag[]
        {
            new MessageSerializerBag(new FakeSerializer(ContentTypes.Json), false)
        });

        var serializer = resolver.Resolve(new Dictionary<string, string>()
        {
            { Headers.ContentType, headerValue}
        });

        Assert.That(serializer, Is.EqualTo(mainSerializer));
    }

    [Test]
    public void MultipleDeserializersWithSameContentTypeShouldThrowException()
    {
        var deserializer1 = new MessageSerializerBag(new FakeSerializer("my/content/type"), false);
        var deserializer2 = new MessageSerializerBag(new FakeSerializer("my/content/type"), false);

        Assert.That(() => new MessageDeserializerResolver(new MessageSerializerBag(new FakeSerializer("xml"), false), new MessageSerializerBag[]
        {
            deserializer1,
            deserializer2
        }), Throws.Exception.TypeOf<Exception>().And.Message.Contains($"Multiple deserializers are registered for content-type '{deserializer1.MessageSerializer.ContentType}'. Remove ambiguous deserializers."));
    }

    class FakeSerializer : IMessageSerializer
    {
        public FakeSerializer(string contentType)
        {
            ContentType = contentType;
        }

        public void Serialize(object message, Stream stream)
        {
        }

        public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null)
        {
            throw new NotImplementedException();
        }

        public string ContentType { get; }
    }
}