namespace NServiceBus.Core.Tests.ManualRegistration;

using System;
using NUnit.Framework;

[TestFixture]
public class RegisterMessageTests
{
    [Test]
    public void RegisterMessage_Generic_Should_Store_Message_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterMessage<TestMessage>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredMessages messages), Is.True);
        Assert.That(messages.MessageTypes, Has.Count.EqualTo(1));
        Assert.That(messages.MessageTypes, Does.Contain(typeof(TestMessage)));
    }

    [Test]
    public void RegisterMessage_NonGeneric_Should_Store_Message_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterMessage(typeof(TestMessage));

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredMessages messages), Is.True);
        Assert.That(messages.MessageTypes, Has.Count.EqualTo(1));
        Assert.That(messages.MessageTypes, Does.Contain(typeof(TestMessage)));
    }

    [Test]
    public void RegisterEvent_Should_Store_Event_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterEvent<TestEvent>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredMessages messages), Is.True);
        Assert.That(messages.MessageTypes, Has.Count.EqualTo(1));
        Assert.That(messages.MessageTypes, Does.Contain(typeof(TestEvent)));
    }

    [Test]
    public void RegisterCommand_Should_Store_Command_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterCommand<TestCommand>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredMessages messages), Is.True);
        Assert.That(messages.MessageTypes, Has.Count.EqualTo(1));
        Assert.That(messages.MessageTypes, Does.Contain(typeof(TestCommand)));
    }

    [Test]
    public void RegisterMessage_Multiple_Should_Store_All()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterMessage<TestMessage>();
        config.RegisterEvent<TestEvent>();
        config.RegisterCommand<TestCommand>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredMessages messages), Is.True);
        Assert.That(messages.MessageTypes, Has.Count.EqualTo(3));
        Assert.That(messages.MessageTypes, Does.Contain(typeof(TestMessage)));
        Assert.That(messages.MessageTypes, Does.Contain(typeof(TestEvent)));
        Assert.That(messages.MessageTypes, Does.Contain(typeof(TestCommand)));
    }

    [Test]
    public void RegisterMessage_Null_MessageType_Should_Throw()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        Assert.Throws<ArgumentNullException>(() => config.RegisterMessage(null));
    }

    public class TestMessage : IMessage
    {
    }

    public class TestEvent : IEvent
    {
    }

    public class TestCommand : ICommand
    {
    }
}

