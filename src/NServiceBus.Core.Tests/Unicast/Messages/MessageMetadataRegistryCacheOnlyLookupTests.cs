namespace NServiceBus.Unicast.Tests;

using System;
using System.Linq;
using NUnit.Framework;
using Unicast.Messages;

[TestFixture]
public class MessageMetadataRegistryCacheOnlyLookupTests
{
    [Test]
    public void Should_return_metadata_for_registered_type()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyMessage)]);

        var found = registry.TryGetMessageMetadata(typeof(MyMessage), out var metadata);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyMessage)));
        }
    }

    [Test]
    public void Should_not_register_unregistered_type_when_lookup_misses()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);

        var found = registry.TryGetMessageMetadata(typeof(MyMessage), out _);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.False);
            Assert.That(registry.GetAllMessages().Select(m => m.MessageType), Does.Not.Contain(typeof(MyMessage)));
        }
    }

    [Test]
    public void Should_return_metadata_for_registered_type_identifier()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyMessage)]);

        var found = registry.TryGetMessageMetadata(typeof(MyMessage).AssemblyQualifiedName, out var metadata);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyMessage)));
        }
    }

    [Test]
    public void Should_return_false_for_unknown_identifier_without_dynamic_type_loading()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);

        var found = registry.TryGetMessageMetadata("Some.Namespace.SomeType, SomeAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void Should_return_false_for_loadable_type_that_is_not_registered()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, false);
        registry.RegisterMessageTypes([typeof(MyMessage)]);

        var found = registry.TryGetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName, out _);

        Assert.That(found, Is.False);
    }

    [Test]
    public void Should_return_false_for_unregistered_type_that_matches_convention()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, false);
        registry.RegisterMessageTypes([typeof(MyMessage)]);

        var found = registry.TryGetMessageMetadata(typeof(OtherMessage), out _);

        Assert.That(found, Is.False);
    }

    public class MyMessage : IMessage;
    public class OtherMessage : IMessage;
}
