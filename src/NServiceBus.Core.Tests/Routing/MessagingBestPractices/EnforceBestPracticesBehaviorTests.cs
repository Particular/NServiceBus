namespace NServiceBus.Core.Tests.Routing.MessagingBestPractices;

using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Testing;

[TestFixture]
public class EnforceBestPracticesBehaviorTests
{
    [Test]
    public void Publish_should_validate_when_enabled()
    {
        var behavior = new EnforcePublishBestPracticesBehavior(CreateValidations());
        var context = new TestableOutgoingPublishContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyCommand), new MyCommand())
        };

        Assert.That(async () => await behavior.Invoke(context, _ => Task.CompletedTask), Throws.Exception);
    }

    [Test]
    public async Task Publish_should_skip_validation_when_disabled()
    {
        var behavior = new EnforcePublishBestPracticesBehavior(CreateValidations());
        var context = new TestableOutgoingPublishContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyCommand), new MyCommand())
        };
        context.Extensions.Set(new EnforceBestPracticesOptions { Enabled = false });

        await behavior.Invoke(context, _ => Task.CompletedTask);
    }

    [Test]
    public void Send_should_validate_when_enabled()
    {
        var behavior = new EnforceSendBestPracticesBehavior(CreateValidations());
        var context = new TestableOutgoingSendContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyEvent), new MyEvent())
        };

        Assert.That(async () => await behavior.Invoke(context, _ => Task.CompletedTask), Throws.Exception);
    }

    [Test]
    public async Task Send_should_skip_validation_when_disabled()
    {
        var behavior = new EnforceSendBestPracticesBehavior(CreateValidations());
        var context = new TestableOutgoingSendContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyEvent), new MyEvent())
        };
        context.Extensions.Set(new EnforceBestPracticesOptions { Enabled = false });

        await behavior.Invoke(context, _ => Task.CompletedTask);
    }

    [Test]
    public void Reply_should_validate_when_enabled()
    {
        var behavior = new EnforceReplyBestPracticesBehavior(CreateValidations());
        var context = new TestableOutgoingReplyContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyCommand), new MyCommand())
        };

        Assert.That(async () => await behavior.Invoke(context, _ => Task.CompletedTask), Throws.Exception);
    }

    [Test]
    public async Task Reply_should_skip_validation_when_disabled()
    {
        var behavior = new EnforceReplyBestPracticesBehavior(CreateValidations());
        var context = new TestableOutgoingReplyContext
        {
            Message = new OutgoingLogicalMessage(typeof(MyCommand), new MyCommand())
        };
        context.Extensions.Set(new EnforceBestPracticesOptions { Enabled = false });

        await behavior.Invoke(context, _ => Task.CompletedTask);
    }

    [Test]
    public void Subscribe_should_validate_every_event_type_when_enabled()
    {
        var behavior = new EnforceSubscribeBestPracticesBehavior(CreateValidations());
        var context = new TestableSubscribeContext
        {
            EventTypes = [typeof(MyEvent), typeof(MyCommand)]
        };

        Assert.That(async () => await behavior.Invoke(context, _ => Task.CompletedTask), Throws.Exception);
    }

    [Test]
    public async Task Subscribe_should_skip_validation_when_disabled()
    {
        var behavior = new EnforceSubscribeBestPracticesBehavior(CreateValidations());
        var context = new TestableSubscribeContext
        {
            EventTypes = [typeof(MyCommand)]
        };
        context.Extensions.Set(new EnforceBestPracticesOptions { Enabled = false });

        await behavior.Invoke(context, _ => Task.CompletedTask);
    }

    [Test]
    public void Unsubscribe_should_validate_event_type_when_enabled()
    {
        var behavior = new EnforceUnsubscribeBestPracticesBehavior(CreateValidations());
        var context = new TestableUnsubscribeContext
        {
            EventType = typeof(MyCommand)
        };

        Assert.That(async () => await behavior.Invoke(context, _ => Task.CompletedTask), Throws.Exception);
    }

    [Test]
    public async Task Unsubscribe_should_skip_validation_when_disabled()
    {
        var behavior = new EnforceUnsubscribeBestPracticesBehavior(CreateValidations());
        var context = new TestableUnsubscribeContext
        {
            EventType = typeof(MyCommand)
        };
        context.Extensions.Set(new EnforceBestPracticesOptions { Enabled = false });

        await behavior.Invoke(context, _ => Task.CompletedTask);
    }

    static Validations CreateValidations() => new(new Conventions());

    class MyCommand : ICommand;
    class MyEvent : IEvent;
}
