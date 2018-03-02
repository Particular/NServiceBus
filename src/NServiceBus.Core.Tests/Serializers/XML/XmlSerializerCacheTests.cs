namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class XmlSerializerCacheTests
    {
        [Test]
        public void InitType_ShouldNotInfinitelyInitializeRecursiveTypes()
        {
            var cache = new XmlSerializerCache();

            cache.InitType(typeof(RecursiveType));

            var fields = cache.typeToFields[typeof(RecursiveType)];
            Assert.AreEqual(typeof(RecursiveType), fields.Single().FieldType);

            var properties = cache.typeToProperties[typeof(RecursiveType)];
            Assert.AreEqual(typeof(RecursiveType), properties.Single().PropertyType);
        }

        [Test]
        public void InitType_ShouldHandleConcurrentInitializations()
        {
            var cache = new XmlSerializerCache();

            Parallel.For(0, 10, _ =>
            {
                cache.InitType(typeof(SimpleType));

                var fields = cache.typeToFields[typeof(SimpleType)];
                var properties = cache.typeToProperties[typeof(SimpleType)];

                Assert.NotNull(fields);
                Assert.NotNull(properties);
                Assert.AreEqual(nameof(SimpleType.SimpleField), fields.Single().Name);
                Assert.AreEqual(nameof(SimpleType.SimpleProperty), properties.Single().Name);

            });
        }
    }

    class SimpleType
    {
#pragma warning disable 649, 169
        public string SimpleField;
#pragma warning restore 649, 169

        public string SimpleProperty { get; set; }

    }

    class RecursiveType
    {
#pragma warning disable 649, 169
        public RecursiveType RecursiveField;
#pragma warning restore 649, 169

        public RecursiveType RecursiveProperty { get; set; }
    }
}