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
    public void Should_copy_from_dictionary()
    {
        var source = new Dictionary<string, string>
        {
            ["Key1"] = "Value1",
            ["Key2"] = "Value2"
        };

        var properties = new ReceiveProperties(source);

        Assert.That(properties, Has.Count.EqualTo(2));
        Assert.That(properties["Key1"], Is.EqualTo("Value1"));
        Assert.That(properties["Key2"], Is.EqualTo("Value2"));
    }

    [Test]
    public void Should_be_copy_of_source()
    {
        var source = new Dictionary<string, string> { ["Key"] = "Value" };
        var properties = new ReceiveProperties(source);

        source["Key"] = "Modified";

        Assert.That(properties["Key"], Is.EqualTo("Value"));
    }
}