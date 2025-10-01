namespace NServiceBus.Core.Tests.Serializers.XML;

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

        Assert.That(cache.typeMembers.ContainsKey(typeof(XElement)), Is.False);
    }

    [Test]
    public void InitType_ShouldNotInfinitelyInitializeRecursiveTypes()
    {
        var cache = new XmlSerializerCache();

        cache.InitType(typeof(RecursiveType));

        var members = cache.typeMembers[typeof(RecursiveType)];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(members.Item1.Single().FieldType, Is.EqualTo(typeof(RecursiveType)));
            Assert.That(members.Item2[0].PropertyType, Is.EqualTo(typeof(RecursiveType)));
            Assert.That(members.Item2[1].PropertyType, Is.EqualTo(typeof(RecursiveType[])));
        }
    }

    [Test]
    public void InitType_ShouldHandleConcurrentInitializations()
    {
        var cache = new XmlSerializerCache();

        Parallel.For(0, 10, _ =>
        {
            cache.InitType(typeof(SimpleType));

            var members = cache.typeMembers[typeof(SimpleType)];

            Assert.That(members, Is.Not.Null);
            Assert.That(members.Item1.Single().Name, Is.EqualTo(nameof(SimpleType.SimpleField)));
            Assert.That(members.Item2.Single().Name, Is.EqualTo(nameof(SimpleType.SimpleProperty)));
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

    public RecursiveType[] RecursiveArrayProperty { get; set; }
}