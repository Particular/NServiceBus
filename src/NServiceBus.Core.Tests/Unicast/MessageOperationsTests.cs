using NServiceBus.Core.Tests.Pipeline;

namespace NServiceBus.Unicast.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class MessageOperationsTests
    {
        [Test]
        public async Task When_sending_message_interface_should_set_interface_as_message_typeAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Send<IMyMessage>(new FakeRootContext(), m => { }, new SendOptions());

            Assert.That(messageOperations.SendPipeline.Context.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public async Task When_sending_message_class_should_set_class_as_message_typeAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions());

            Assert.That(messageOperations.SendPipeline.Context.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public async Task When_sending_should_generate_message_id_and_set_message_id_headerAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions());

            var messageId = messageOperations.SendPipeline.Context.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, messageOperations.SendPipeline.Context.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_sending_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
        {
            const string expectedMessageID = "expected message id";

            var messageOperations = new TestableMessageOperations();

            var sendOptions = new SendOptions();
            sendOptions.SetMessageId(expectedMessageID);
            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions);

            Assert.AreEqual(expectedMessageID, messageOperations.SendPipeline.Context.MessageId);
            Assert.AreEqual(expectedMessageID, messageOperations.SendPipeline.Context.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_sending_should_clone_headersAsync()
        {
            var messageOperations = new TestableMessageOperations();

            var sendOptions = new SendOptions();
            sendOptions.SetHeader("header1", "header1 value");
            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions);
            messageOperations.SendPipeline.Context.Headers.Add("header2", "header2 value");
            messageOperations.SendPipeline.Context.Headers["header1"] = "updated header1 value";

            var optionsHeaders = sendOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        [Test]
        public async Task When_replying_message_interface_should_set_interface_as_message_typeAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Reply<IMyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

            Assert.That(messageOperations.ReplyPipeline.Context.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public async Task When_replying_message_class_should_set_class_as_message_typeAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

            Assert.That(messageOperations.ReplyPipeline.Context.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public async Task When_replying_should_generate_message_id_and_set_message_id_headerAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

            var messageId = messageOperations.ReplyPipeline.Context.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, messageOperations.ReplyPipeline.Context.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_replying_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
        {
            const string expectedMessageID = "expected message id";

            var messageOperations = new TestableMessageOperations();

            var replyOptions = new ReplyOptions();
            replyOptions.SetMessageId(expectedMessageID);
            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions);

            Assert.AreEqual(expectedMessageID, messageOperations.ReplyPipeline.Context.MessageId);
            Assert.AreEqual(expectedMessageID, messageOperations.ReplyPipeline.Context.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_replying_should_clone_headersAsync()
        {
            var messageOperations = new TestableMessageOperations();

            var replyOptions = new ReplyOptions();
            replyOptions.SetHeader("header1", "header1 value");
            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions);
            messageOperations.ReplyPipeline.Context.Headers.Add("header2", "header2 value");
            messageOperations.ReplyPipeline.Context.Headers["header1"] = "updated header1 value";

            var optionsHeaders = replyOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        [Test]
        public async Task When_publishing_event_interface_should_set_interface_as_message_typeAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Publish<IMyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

            Assert.That(messageOperations.PublishPipeline.Context.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public async Task When_publishing_event_class_should_set_class_as_message_typeAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

            Assert.That(messageOperations.PublishPipeline.Context.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public async Task When_publishing_should_generate_message_id_and_set_message_id_headerAsync()
        {
            var messageOperations = new TestableMessageOperations();

            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

            var messageId = messageOperations.PublishPipeline.Context.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, messageOperations.PublishPipeline.Context.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_publishing_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
        {
            const string expectedMessageID = "expected message id";

            var messageOperations = new TestableMessageOperations();

            var publishOptions = new PublishOptions();
            publishOptions.SetMessageId(expectedMessageID);
            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions);

            Assert.AreEqual(expectedMessageID, messageOperations.PublishPipeline.Context.MessageId);
            Assert.AreEqual(expectedMessageID, messageOperations.PublishPipeline.Context.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_publishing_should_clone_headers()
        {
            var messageOperations = new TestableMessageOperations();

            var publishOptions = new PublishOptions();
            publishOptions.SetHeader("header1", "header1 value");
            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions);
            messageOperations.PublishPipeline.Context.Headers.Add("header2", "header2 value");
            messageOperations.PublishPipeline.Context.Headers["header1"] = "updated header1 value";

            var optionsHeaders = publishOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        public interface IMyMessage
        {
        }

        class MyMessage
        {
        }
    }
}