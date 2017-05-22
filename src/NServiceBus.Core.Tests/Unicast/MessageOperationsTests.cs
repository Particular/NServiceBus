namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;
    using Pipeline;
    using Testing;

    [TestFixture]
    public class MessageOperationsTests
    {
        [Test]
        public void When_sending_message_interface_should_set_interface_as_message_type()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var context = CreateContext(sendPipeline);

            MessageOperations.Send<IMyMessage>(context, m => { }, new SendOptions());

            Assert.That(sendPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public void When_sending_message_class_should_set_class_as_message_type()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var context = CreateContext(sendPipeline);

            MessageOperations.Send<MyMessage>(context, m => { }, new SendOptions());

            Assert.That(sendPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public void When_sending_should_generate_message_id_and_set_message_id_header()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var context = CreateContext(sendPipeline);

            MessageOperations.Send<MyMessage>(context, m => { }, new SendOptions());

            var messageId = sendPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, sendPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_sending_with_user_defined_message_id_should_set_defined_id_and_header()
        {
            const string expectedMessageID = "expected message id";

            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var context = CreateContext(sendPipeline);
            var sendOptions = new SendOptions();
            sendOptions.SetMessageId(expectedMessageID);

            MessageOperations.Send<MyMessage>(context, m => { }, sendOptions);

            Assert.AreEqual(expectedMessageID, sendPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, sendPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_sending_should_clone_headers()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var context = CreateContext(sendPipeline);
            var sendOptions = new SendOptions();
            sendOptions.SetHeader("header1", "header1 value");

            MessageOperations.Send<MyMessage>(context, m => { }, sendOptions);
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
            var context = CreateContext(replyPipeline);

            MessageOperations.Reply<IMyMessage>(context, m => { }, new ReplyOptions());

            Assert.That(replyPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public void When_replying_message_class_should_set_class_as_message_type()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var context = CreateContext(replyPipeline);

            MessageOperations.Reply<MyMessage>(context, m => { }, new ReplyOptions());

            Assert.That(replyPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public void When_replying_should_generate_message_id_and_set_message_id_header()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var context = CreateContext(replyPipeline);

            MessageOperations.Reply<MyMessage>(context, m => { }, new ReplyOptions());

            var messageId = replyPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, replyPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_replying_with_user_defined_message_id_should_set_defined_id_and_header()
        {
            const string expectedMessageID = "expected message id";

            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var context = CreateContext(replyPipeline);
            var replyOptions = new ReplyOptions();
            replyOptions.SetMessageId(expectedMessageID);

            MessageOperations.Reply<MyMessage>(context, m => { }, replyOptions);

            Assert.AreEqual(expectedMessageID, replyPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, replyPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_replying_should_clone_headers()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var context = CreateContext(replyPipeline);
            var replyOptions = new ReplyOptions();
            replyOptions.SetHeader("header1", "header1 value");

            MessageOperations.Reply<MyMessage>(context, m => { }, replyOptions);
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
            var context = CreateContext(publishPipeline);

            MessageOperations.Publish<IMyMessage>(context, m => { }, new PublishOptions());

            Assert.That(publishPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        }

        [Test]
        public void When_publishing_event_class_should_set_class_as_message_type()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var context = CreateContext(publishPipeline);

            MessageOperations.Publish<MyMessage>(context, m => { }, new PublishOptions());

            Assert.That(publishPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        [Test]
        public void When_publishing_should_generate_message_id_and_set_message_id_header()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var context = CreateContext(publishPipeline);

            MessageOperations.Publish<MyMessage>(context, m => { }, new PublishOptions());

            var messageId = publishPipeline.ReceivedContext.MessageId;
            Assert.IsNotNull(messageId);
            Assert.AreEqual(messageId, publishPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_publishing_with_user_defined_message_id_should_set_defined_id_and_header()
        {
            const string expectedMessageID = "expected message id";

            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var context = CreateContext(publishPipeline);
            var publishOptions = new PublishOptions();
            publishOptions.SetMessageId(expectedMessageID);

            MessageOperations.Publish<MyMessage>(context, m => { }, publishOptions);

            Assert.AreEqual(expectedMessageID, publishPipeline.ReceivedContext.MessageId);
            Assert.AreEqual(expectedMessageID, publishPipeline.ReceivedContext.Headers[Headers.MessageId]);
        }

        [Test]
        public void When_publishing_should_clone_headers()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var context = CreateContext(publishPipeline);
            var publishOptions = new PublishOptions();
            publishOptions.SetHeader("header1", "header1 value");

            MessageOperations.Publish<MyMessage>(context, m => { }, publishOptions);
            publishPipeline.ReceivedContext.Headers.Add("header2", "header2 value");
            publishPipeline.ReceivedContext.Headers["header1"] = "updated header1 value";

            var optionsHeaders = publishOptions.GetHeaders();
            Assert.AreEqual(1, optionsHeaders.Count);
            Assert.AreEqual("header1 value", optionsHeaders["header1"]);
        }

        IBehaviorContext CreateContext<TContext>(IPipeline<TContext> pipeline) where TContext : IBehaviorContext
        {
            var pipelineCache = new FakePipelineCache();
            pipelineCache.RegisterPipeline(pipeline);

            var context = new TestableMessageHandlerContext();
            context.Builder.Register<IMessageMapper>(() => new MessageMapper());
            context.Extensions.Set<IPipelineCache>(pipelineCache);

            return context;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public interface IMyMessage
        {
        }

        class MyMessage
        {
        }

        class FakePipelineCache : IPipelineCache
        {
            Dictionary<Type, object> pipelines = new Dictionary<Type, object>();

            public void RegisterPipeline<TContext>(IPipeline<TContext> pipeline) where TContext : IBehaviorContext
            {
                pipelines.Add(typeof(TContext), pipeline);
            }

            public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext
            {
                return pipelines[typeof(TContext)] as IPipeline<TContext>;
            }
        }

        class FakePipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
        {
            public TContext ReceivedContext { get; set; }

            public Task Invoke(TContext context)
            {
                ReceivedContext = context;
                return TaskEx.CompletedTask;
            }
        }
    }
}