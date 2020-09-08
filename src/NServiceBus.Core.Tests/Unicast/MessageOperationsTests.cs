namespace NServiceBus.Unicast.Tests
{
    using System.Threading;
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
        public void When_sending_message_interface_should_set_interface_as_message_type()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            messageOperations.Send<IMyMessage>(new FakeRootContext(), m => { }, new SendOptions(), CancellationToken.None);

            Assert.That(sendPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public void When_sending_message_class_should_set_class_as_message_type()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions(), CancellationToken.None);

            Assert.That(sendPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public void When_sending_should_generate_message_id_and_set_message_id_header()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, new SendOptions(), CancellationToken.None);

            var messageId = sendPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, sendPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_sending_with_user_defined_message_id_should_set_defined_id_and_header()
        {
            const string expectedMessageID = "expected message id";

            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            var sendOptions = new SendOptions();
            sendOptions.SetMessageId(expectedMessageID);
            messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions, CancellationToken.None);

            Assert.AreEqual(expectedMessageID, sendPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, sendPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_sending_should_clone_headers()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var messageOperations = CreateMessageOperations(sendPipeline: sendPipeline);

            var sendOptions = new SendOptions();
            sendOptions.SetHeader("header1", "header1 value");
            messageOperations.Send<MyMessage>(new FakeRootContext(), m => { }, sendOptions, CancellationToken.None);
            sendPipeline.ReceivedContext.Headers.Add("header2", "header2 value");
            sendPipeline.ReceivedContext.Headers["header1"] = "updated header1 value";

            var optionsHeaders = sendOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        [Test]
        public void When_replying_message_interface_should_set_interface_as_message_type()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            messageOperations.Reply<IMyMessage>(new FakeRootContext(), m => { }, new ReplyOptions(), CancellationToken.None);

            Assert.That(replyPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public void When_replying_message_class_should_set_class_as_message_type()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions(), CancellationToken.None);

            Assert.That(replyPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public void When_replying_should_generate_message_id_and_set_message_id_header()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, new ReplyOptions(), CancellationToken.None);

            var messageId = replyPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, replyPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_replying_with_user_defined_message_id_should_set_defined_id_and_header()
        {
            const string expectedMessageID = "expected message id";

            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            var replyOptions = new ReplyOptions();
            replyOptions.SetMessageId(expectedMessageID);
            messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions, CancellationToken.None);

            Assert.AreEqual(expectedMessageID, replyPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, replyPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_replying_should_clone_headers()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var messageOperations = CreateMessageOperations(replyPipeline: replyPipeline);

            var replyOptions = new ReplyOptions();
            replyOptions.SetHeader("header1", "header1 value");
            messageOperations.Reply<MyMessage>(new FakeRootContext(), m => { }, replyOptions, CancellationToken.None);
            replyPipeline.ReceivedContext.Headers.Add("header2", "header2 value");
            replyPipeline.ReceivedContext.Headers["header1"] = "updated header1 value";

            var optionsHeaders = replyOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        [Test]
        public void When_publishing_event_interface_should_set_interface_as_message_type()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            messageOperations.Publish<IMyMessage>(new FakeRootContext(), m => { }, new PublishOptions(), CancellationToken.None);

            Assert.That(publishPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public void When_publishing_event_class_should_set_class_as_message_type()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions(), CancellationToken.None);

            Assert.That(publishPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public void When_publishing_should_generate_message_id_and_set_message_id_header()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, new PublishOptions(), CancellationToken.None);

            var messageId = publishPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, publishPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_publishing_with_user_defined_message_id_should_set_defined_id_and_header()
        {
            const string expectedMessageID = "expected message id";

            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            var publishOptions = new PublishOptions();
            publishOptions.SetMessageId(expectedMessageID);
            messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions, CancellationToken.None);

            Assert.AreEqual(expectedMessageID, publishPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, publishPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_publishing_should_clone_headers()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var messageOperations = CreateMessageOperations(publishPipeline: publishPipeline);

            var publishOptions = new PublishOptions();
            publishOptions.SetHeader("header1", "header1 value");
            messageOperations.Publish<MyMessage>(new FakeRootContext(), m => { }, publishOptions, CancellationToken.None);
            publishPipeline.ReceivedContext.Headers.Add("header2", "header2 value");
            publishPipeline.ReceivedContext.Headers["header1"] = "updated header1 value";

            var optionsHeaders = publishOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IMyMessage
        {
        }

        class MyMessage
        {
        }

        class FakePipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
        {
            public TContext ReceivedContext { get; set; }

            public Task Invoke(TContext context, CancellationToken cancellationToken)
            {
                ReceivedContext = context;
                return Task.CompletedTask;
            }
        }
    }
}