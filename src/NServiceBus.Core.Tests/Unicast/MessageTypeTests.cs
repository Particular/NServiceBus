namespace NServiceBus.Unicast.Tests;

using System;
using NUnit.Framework;

[TestFixture]
public class MessageTypeTests
{
    [Test]
    public void Should_parse_types()
    {
        var messageType = new Subscriptions.MessageType(typeof(TestMessage));

        Assert.That(typeof(TestMessage).FullName, Is.EqualTo(messageType.TypeName));
        Assert.That(typeof(TestMessage).Assembly.GetName().Version, Is.EqualTo(messageType.Version));
    }

    [Test]
    public void Should_parse_AssemblyQualifiedName()
    {
        var messageType = new Subscriptions.MessageType(typeof(TestMessage).AssemblyQualifiedName);

        Assert.That(typeof(TestMessage).FullName, Is.EqualTo(messageType.TypeName));
        Assert.That(typeof(TestMessage).Assembly.GetName().Version, Is.EqualTo(messageType.Version));
    }

    [Test]
    public void Should_parse_version_strings()
    {
        var messageType = new Subscriptions.MessageType("TestMessage", "1.2.3.4");

        Assert.That("TestMessage", Is.EqualTo(messageType.TypeName));
        Assert.That(new Version(1, 2, 3, 4), Is.EqualTo(messageType.Version));
    }


    class TestMessage
    {

    }
}