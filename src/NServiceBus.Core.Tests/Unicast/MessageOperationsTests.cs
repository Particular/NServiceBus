namespace NServiceBus.Unicast.Tests
{
    using System.Threading.Tasks;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;
    using Pipeline;

    [TestFixture]
    public class MessageOperationsTests
    {
        MessageOperations CreateMessageOperations(
            FakePipeline<IOutgoingPublishContext> publishPipeline = null,
            FakePipeline<IOutgoingSendContext> sendPipeline = null,
            FakePipeline<IOutgoingReplyContext> replyPipeline = null,
            FakePipeline<ISubscribeContext> subscribePipeline = null,
            FakePipeline<IUnsubscribeContext> subscribeContext = null)
        {
            return new MessageOperations(
                new MessageMapper(),
                publishPipeline ?? new FakePipeline<IOutgoingPublishContext>(),
                sendPipeline ?? new FakePipeline<IOutgoingSendContext>(),
                replyPipeline ?? new FakePipeline<IOutgoingReplyContext>(),
                subscribePipeline ?? new FakePipeline<ISubscribeContext>(),
                subscribeContext ?? new FakePipeline<IUnsubscribeContext>());
        }

        [Test]
        public async Task When_sending_message_interface_should_set_interface_as_message_typeAsync()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            await messageOperations.Send<IMyMessage>(new FakeRootContext(), m => { }, new SendOptions());

            Assert.That(sendPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public async Task When_sending_message_class_should_set_class_as_message_typeAsync()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions());

            Assert.That(sendPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public async Task When_sending_should_generate_message_id_and_set_message_id_headerAsync()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions());

            var messageId = sendPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, sendPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_sending_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
        {
            const string expectedMessageID = "expected message id";

            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            var sendOptions = new SendOptions();
            sendOptions.SetMessageId(expectedMessageID);
            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions);

            Assert.AreEqual(expectedMessageID, sendPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, sendPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_sending_should_clone_headersAsync()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            var sendOptions = new SendOptions();
            sendOptions.SetHeader("header1", "header1 value");
            await messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions);
            sendPipeline.ReceivedContext.Headers.Add("header2", "header2 value");
            sendPipeline.ReceivedContext.Headers["header1"] = "updated header1 value";

            var optionsHeaders = sendOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        [Test]
        public async Task When_replying_message_interface_should_set_interface_as_message_typeAsync()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            await messageOperations.Reply<IMyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

            Assert.That(replyPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public async Task When_replying_message_class_should_set_class_as_message_typeAsync()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

            Assert.That(replyPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public async Task When_replying_should_generate_message_id_and_set_message_id_headerAsync()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions());

            var messageId = replyPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, replyPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_replying_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
        {
            const string expectedMessageID = "expected message id";

            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            var replyOptions = new ReplyOptions();
            replyOptions.SetMessageId(expectedMessageID);
            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions);

            Assert.AreEqual(expectedMessageID, replyPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, replyPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_replying_should_clone_headersAsync()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            var replyOptions = new ReplyOptions();
            replyOptions.SetHeader("header1", "header1 value");
            await messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions);
            replyPipeline.ReceivedContext.Headers.Add("header2", "header2 value");
            replyPipeline.ReceivedContext.Headers["header1"] = "updated header1 value";

            var optionsHeaders = replyOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        [Test]
        public async Task When_publishing_event_interface_should_set_interface_as_message_typeAsync()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            await messageOperations.Publish<IMyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

            Assert.That(publishPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public async Task When_publishing_event_class_should_set_class_as_message_typeAsync()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

            Assert.That(publishPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public async Task When_publishing_should_generate_message_id_and_set_message_id_headerAsync()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions());

            var messageId = publishPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, publishPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_publishing_with_user_defined_message_id_should_set_defined_id_and_headerAsync()
        {
            const string expectedMessageID = "expected message id";

            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            var publishOptions = new PublishOptions();
            publishOptions.SetMessageId(expectedMessageID);
            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions);

            Assert.AreEqual(expectedMessageID, publishPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, publishPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public async Task When_publishing_should_clone_headers()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            var publishOptions = new PublishOptions();
            publishOptions.SetHeader("header1", "header1 value");
            await messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions);
            publishPipeline.ReceivedContext.Headers.Add("header2", "header2 value");
            publishPipeline.ReceivedContext.Headers["header1"] = "updated header1 value";

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

        class FakePipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
        {
            public TContext ReceivedContext { get; set; }

            public Task Invoke(TContext context)
            {
                ReceivedContext = context;
                return Task.CompletedTask;
            }
        }
    }
}