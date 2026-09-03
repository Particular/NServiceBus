namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System;
using MessageInterfaces;
using MessageInterfaces.MessageMapper.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;
using Unicast.Messages;

[TestFixture]
public class IncomingLogicalMessageContextTests
{
    [Test]
    public void Updating_the_message_to_a_new_type_should_update_the_MessageType()
    {
        var context = CreateContext(typeof(MyDifferentMessage));

        var differentMessage = new MyDifferentMessage();
        context.UpdateMessageInstance<MyDifferentMessage>(differentMessage);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Message.MessageType, Is.EqualTo(typeof(MyDifferentMessage)));
            Assert.That(context.Message.Instance, Is.SameAs(differentMessage));
        }
    }

    [Test]
    public void Updating_the_existing_instance_with_a_different_explicit_type_should_use_that_type()
    {
        var message = new MySubMessage();
        var context = CreateContext(typeof(MySubMessage), message);

        context.UpdateMessageInstance<MyDifferentMessage>(message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Message.MessageType, Is.EqualTo(typeof(MyDifferentMessage)));
            Assert.That(context.Message.Instance, Is.SameAs(message));
        }
    }

    [Test]
    public void Updating_the_existing_instance_with_the_same_type_should_preserve_the_metadata()
    {
        var message = new MyDifferentMessage();
        var context = CreateContext(typeof(MyDifferentMessage), message);

        var metadataBefore = context.Message.Metadata;

        context.UpdateMessageInstance<MyDifferentMessage>(message);

        Assert.That(context.Message.Metadata, Is.SameAs(metadataBefore));
    }

    [Test]
    public void Updating_with_an_explicit_type_that_is_not_assignable_should_throw()
    {
        var context = CreateContext(typeof(MyDifferentMessage));

        Assert.Throws<ArgumentException>(() => context.UpdateMessageInstance(new MyDifferentMessage(), typeof(string)));
    }

    [Test]
    public void Updating_with_a_null_instance_should_throw()
    {
        var context = CreateContext(typeof(MyDifferentMessage));

        Assert.Throws<ArgumentNullException>(() => context.UpdateMessageInstance(null!, typeof(MyDifferentMessage)));
    }

    [Test]
    public void Updating_with_a_null_message_type_should_throw()
    {
        var context = CreateContext(typeof(MyDifferentMessage));

        Assert.Throws<ArgumentNullException>(() => context.UpdateMessageInstance(new MyDifferentMessage(), null!));
    }

    static IncomingLogicalMessageContext CreateContext(Type messageType, object instance = null)
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyDifferentMessage), typeof(MySubMessage)]);
        var services = new ServiceCollection();
        services.AddSingleton(registry);
        services.AddSingleton<LogicalMessageFactory>();
        services.AddSingleton<IMessageMapper>(new TrimmingSafeMessageMapper());
        IServiceProvider provider = services.BuildServiceProvider();

        var parentContext = new TestableIncomingPhysicalMessageContext();
        parentContext.Extensions.Set(provider);

        instance ??= new MyDifferentMessage();

        var logicalMessage = new LogicalMessage(registry.GetMessageMetadata(messageType), instance);
        var context = new IncomingLogicalMessageContext(logicalMessage, parentContext);

        return context;
    }

    class MyDifferentMessage : IMessage
    { }

    class MySubMessage : MyDifferentMessage
    { }
}
