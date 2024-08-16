namespace NServiceBus.Unicast.Tests;

using System.Threading.Tasks;
using Core.Tests.Pipeline;
using NUnit.Framework;

[TestFixture]
public class MessageOperationsTests
{
    [Test]
    public async Task When_sending_message_interface_should_set_interface_as_message_typeAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Send<IMyMessage>(new FakeRootContext(), m => { }, new SendOptions());

        Assert.That(messageOperations.SendPipeline.LastContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
    }

    [Test]
    public async Task When_sending_message_class_should_set_class_as_message_typeAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions());

        Assert.That(messageOperations.SendPipeline.LastContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [Test]
    public async Task When_sending_should_generate_message_id_and_set_message_id_headerAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions());

        var messageId = messageOperations.SendPipeline.LastContext.MessageId;
        Assert.IsNotNull(messageId);
        Assert.That(messageOperations.SendPipeline.LastContext.Headers[Headers.MessageId], Is.EqualTo(messageId));
    }

    [Test]
    public async Task When_sending_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
    {
        const string expectedMessageID = "expected message id";

        var messageOperations = new TestableMessageOperations();

        var sendOptions = new SendOptions();
        sendOptions.SetMessageId(expectedMessageID);
        await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions);

        Assert.That(messageOperations.SendPipeline.LastContext.MessageId, Is.EqualTo(expectedMessageID));
        Assert.That(messageOperations.SendPipeline.LastContext.Headers[Headers.MessageId], Is.EqualTo(expectedMessageID));
    }

    [Test]
    public async Task When_sending_should_clone_headersAsync()
    {
        var messageOperations = new TestableMessageOperations();

        var sendOptions = new SendOptions();
        sendOptions.SetHeader("header1", "header1 value");
        await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions);
        messageOperations.SendPipeline.LastContext.Headers.Add("header2", "header2 value");
        messageOperations.SendPipeline.LastContext.Headers["header1"] = "updated header1 value";

        var optionsHeaders = sendOptions.GetHeaders();
        Assert.That(optionsHeaders.Count, Is.EqualTo(1));
        Assert.That(optionsHeaders["header1"], Is.EqualTo("header1 value"));
    }

    [Test]
    public async Task When_replying_message_interface_should_set_interface_as_message_typeAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Reply<IMyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

        Assert.That(messageOperations.ReplyPipeline.LastContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
    }

    [Test]
    public async Task When_replying_message_class_should_set_class_as_message_typeAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

        Assert.That(messageOperations.ReplyPipeline.LastContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [Test]
    public async Task When_replying_should_generate_message_id_and_set_message_id_headerAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

        var messageId = messageOperations.ReplyPipeline.LastContext.MessageId;
        Assert.IsNotNull(messageId);
        Assert.That(messageOperations.ReplyPipeline.LastContext.Headers[Headers.MessageId], Is.EqualTo(messageId));
    }

    [Test]
    public async Task When_replying_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
    {
        const string expectedMessageID = "expected message id";

        var messageOperations = new TestableMessageOperations();

        var replyOptions = new ReplyOptions();
        replyOptions.SetMessageId(expectedMessageID);
        await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions);

        Assert.That(messageOperations.ReplyPipeline.LastContext.MessageId, Is.EqualTo(expectedMessageID));
        Assert.That(messageOperations.ReplyPipeline.LastContext.Headers[Headers.MessageId], Is.EqualTo(expectedMessageID));
    }

    [Test]
    public async Task When_replying_should_clone_headersAsync()
    {
        var messageOperations = new TestableMessageOperations();

        var replyOptions = new ReplyOptions();
        replyOptions.SetHeader("header1", "header1 value");
        await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions);
        messageOperations.ReplyPipeline.LastContext.Headers.Add("header2", "header2 value");
        messageOperations.ReplyPipeline.LastContext.Headers["header1"] = "updated header1 value";

        var optionsHeaders = replyOptions.GetHeaders();
        Assert.That(optionsHeaders.Count, Is.EqualTo(1));
        Assert.That(optionsHeaders["header1"], Is.EqualTo("header1 value"));
    }

    [Test]
    public async Task When_publishing_event_interface_should_set_interface_as_message_typeAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Publish<IMyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

        Assert.That(messageOperations.PublishPipeline.LastContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
    }

    [Test]
    public async Task When_publishing_event_class_should_set_class_as_message_typeAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

        Assert.That(messageOperations.PublishPipeline.LastContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [Test]
    public async Task When_publishing_should_generate_message_id_and_set_message_id_headerAsync()
    {
        var messageOperations = new TestableMessageOperations();

        await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

        var messageId = messageOperations.PublishPipeline.LastContext.MessageId;
        Assert.IsNotNull(messageId);
        Assert.That(messageOperations.PublishPipeline.LastContext.Headers[Headers.MessageId], Is.EqualTo(messageId));
    }

    [Test]
    public async Task When_publishing_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
    {
        const string expectedMessageID = "expected message id";

        var messageOperations = new TestableMessageOperations();

        var publishOptions = new PublishOptions();
        publishOptions.SetMessageId(expectedMessageID);
        await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions);

        Assert.That(messageOperations.PublishPipeline.LastContext.MessageId, Is.EqualTo(expectedMessageID));
        Assert.That(messageOperations.PublishPipeline.LastContext.Headers[Headers.MessageId], Is.EqualTo(expectedMessageID));
    }

    [Test]
    public async Task When_publishing_should_clone_headers()
    {
        var messageOperations = new TestableMessageOperations();

        var publishOptions = new PublishOptions();
        publishOptions.SetHeader("header1", "header1 value");
        await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions);
        messageOperations.PublishPipeline.LastContext.Headers.Add("header2", "header2 value");
        messageOperations.PublishPipeline.LastContext.Headers["header1"] = "updated header1 value";

        var optionsHeaders = publishOptions.GetHeaders();
        Assert.That(optionsHeaders.Count, Is.EqualTo(1));
        Assert.That(optionsHeaders["header1"], Is.EqualTo("header1 value"));
    }

    public interface IMyMessage
    {
    }

    class MyMessage
    {
    }
}