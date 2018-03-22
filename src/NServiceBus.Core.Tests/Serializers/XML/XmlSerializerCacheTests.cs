namespace NServiceBus.Core.Tests.Serializers.XML
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class XmlSerializerCacheTests
    {
        [Test]
        public void InitType_ShouldNotInitializeXContainerTypes()
        {
            var cache = new XmlSerializerCache();

            cache.InitType(typeof(XElement));

            Assert.IsFalse(cache.typeMembers.ContainsKey(typeof(XElement)));
        }

        [Test]
        public void InitType_ShouldNotInfinitelyInitializeRecursiveTypes()
        {
            var cache = new XmlSerializerCache();

            cache.InitType(typeof(RecursiveType));

            var members = cache.typeMembers[typeof(RecursiveType)];
            Assert.AreEqual(typeof(RecursiveType), members.Item1.Single().FieldType);
            Assert.AreEqual(typeof(RecursiveType), members.Item2.Single().PropertyType);
        }

        [Test]
        public void InitType_ShouldHandleConcurrentInitializations()
        {
            var cache = new XmlSerializerCache();

            Parallel.For(0, 10, _ =>
            {
                cache.InitType(typeof(SimpleType));

                var members = cache.typeMembers[typeof(SimpleType)];
                Assert.NotNull(members);
                Assert.AreEqual(nameof(SimpleType.SimpleField), members.Item1.Single().Name);
                Assert.AreEqual(nameof(SimpleType.SimpleProperty), members.Item2.Single().Name);
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