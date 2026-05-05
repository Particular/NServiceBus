namespace NServiceBus.Core.Tests.Transports;

using System.Collections.Generic;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class ReceivePropertiesTests
{
    [Test]
    public void Should_initialize_empty()
    {
        var properties = new ReceiveProperties();
        Assert.That(properties, Is.Empty);
    }

    [Test]
    public void Should_wrap_provided_dictionary()
    {
        var source = new Dictionary<string, string>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2"
        };

        var properties = new ReceiveProperties(source);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(properties, Has.Count.EqualTo(2));
            Assert.That(properties["Key1"], Is.EqualTo("Value1"));
            Assert.That(properties["Key2"], Is.EqualTo("Value2"));
        }
    }

    [Test]
    public void Should_be_same_reference_as_source()
    {
        var source = new Dictionary<string, string> { ["Key"] = "Value" };
        var properties = new ReceiveProperties(source);

        Assert.That(properties, Is.Not.Empty);
        Assert.That(properties["Key"], Is.EqualTo("Value"));
    }

    [Test]
    public void Empty_should_be_immutable_singleton()
    {
        var empty1 = ReceiveProperties.Empty;
        var empty2 = ReceiveProperties.Empty;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(empty1, Is.SameAs(empty2));
            Assert.That(empty1, Is.Empty);
        }
    }

    [Test]
    public void Should_be_readonly_via_interface()
    {
        var properties = new ReceiveProperties(new Dictionary<string, string> { ["Key"] = "Value" });

        Assert.That(properties, Is.InstanceOf<IReadOnlyDictionary<string, string>>());
        Assert.That(properties.ContainsKey("Key"), Is.True);
        Assert.That(properties.TryGetValue("Key", out var value), Is.True);
        Assert.That(value, Is.EqualTo("Value"));
    }
}