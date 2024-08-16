namespace NServiceBus.Testing.Tests.Fakes;

using System.Collections.Generic;
using System.Threading.Tasks;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class TestableMessageHandlerContextTests
{
    [Test]
    public async Task Send_ShouldContainMessageInSentMessages()
    {
        var context = new TestableMessageHandlerContext();
        var messageInstance = new TestMessage();
        var sendOptions = new SendOptions();

        await context.Send(messageInstance, sendOptions);

        Assert.That(context.SentMessages.Length, Is.EqualTo(1));
        Assert.That(context.SentMessages[0].Message, Is.SameAs(messageInstance));
        Assert.That(context.SentMessages[0].Options, Is.SameAs(sendOptions));
    }

    [Test]
    public async Task Send_ShouldInvokeMessageInitializer()
    {
        var context = new TestableMessageHandlerContext();

        await context.Send<ITestMessage>(m => m.Value = "initialized value");

        Assert.That(context.SentMessages[0].Message<ITestMessage>().Value, Is.EqualTo("initialized value"));
    }

    [Test]
    public async Task Publish_ShouldContainMessageInPublishedMessages()
    {
        var context = new TestableMessageHandlerContext();
        var messageInstance = new TestMessage();
        var publishOptions = new PublishOptions();

        await context.Publish(messageInstance, publishOptions);

        Assert.That(context.PublishedMessages.Length, Is.EqualTo(1));
        Assert.That(context.PublishedMessages[0].Message, Is.SameAs(messageInstance));
        Assert.That(context.PublishedMessages[0].Options, Is.SameAs(publishOptions));
    }

    [Test]
    public async Task Publish_ShouldInvokeMessageInitializer()
    {
        var context = new TestableMessageHandlerContext();

        await context.Publish<ITestMessage>(m => m.Value = "initialized value");

        Assert.That(context.PublishedMessages[0].Message<ITestMessage>().Value, Is.EqualTo("initialized value"));
    }

    [Test]
    public async Task Reply_ShouldContainMessageInRepliedMessages()
    {
        var context = new TestableMessageHandlerContext();
        var messageInstance = new TestMessage();
        var publishOptions = new ReplyOptions();

        await context.Reply(messageInstance, publishOptions);

        Assert.That(context.RepliedMessages.Length, Is.EqualTo(1));
        Assert.That(context.RepliedMessages[0].Message, Is.SameAs(messageInstance));
        Assert.That(context.RepliedMessages[0].Options, Is.SameAs(publishOptions));
    }

    [Test]
    public async Task Reply_ShouldInvokeMessageInitializer()
    {
        var context = new TestableMessageHandlerContext();

        await context.Reply<ITestMessage>(m => m.Value = "initialized value");

        Assert.That(context.RepliedMessages[0].Message<ITestMessage>().Value, Is.EqualTo("initialized value"));
    }

    [Test]
    public async Task ForwardCurrentMessageTo_ShouldContainDestinationsInForwardDestinations()
    {
        var context = new TestableMessageHandlerContext();

        await context.ForwardCurrentMessageTo("destination1");
        await context.ForwardCurrentMessageTo("destination2");

        Assert.Contains("destination1", context.ForwardedMessages);
        Assert.Contains("destination2", context.ForwardedMessages);
    }

    [Test]
    public void DoNotContinueDispatchingCurrentMessageToHandlers_WhenNotCalled_ShouldNotIndicateInvocation()
    {
        var context = new TestableMessageHandlerContext();

        Assert.That(context.DoNotContinueDispatchingCurrentMessageToHandlersWasCalled, Is.False);
    }

    [Test]
    public void DoNotContinueDispatchingCurrentMessageToHandlers_WhenCalled_ShouldIndicateInvocation()
    {
        var context = new TestableMessageHandlerContext();

        context.DoNotContinueDispatchingCurrentMessageToHandlers();

        Assert.That(context.DoNotContinueDispatchingCurrentMessageToHandlersWasCalled, Is.True);
    }

    [Test]
    public void ShouldAllowSettingMessageProperties()
    {
        var context = new TestableMessageHandlerContext
        {
            MessageId = "custom message id",
            ReplyToAddress = "custom reply address",
            MessageHeaders = new Dictionary<string, string>()
        };
        context.MessageHeaders.Add("custom header", "custom value");
        context.Extensions = new ContextBag();
    }

    class TestMessage
    {
    }

    public interface ITestMessage
    {
        string Value { get; set; }
    }
}