namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Pipeline;
    using Testing;
    using PublishOptions = NServiceBus.PublishOptions;
    using ReplyOptions = NServiceBus.ReplyOptions;
    using SendOptions = NServiceBus.SendOptions;

    [TestFixture]
    public class MessageOperationsTests
    {
        [Test]
        public void When_sending_message_class_should_set_class_as_message_type()
        {
            var sendPipeline = new FakePipeline<IOutgoingSendContext>();
            var context = CreateContext(sendPipeline);

            MessageOperations.Send(context, new MyMessage(), new SendOptions());

            Assert.That(sendPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }


        [Test]
        public void When_replying_message_class_should_set_class_as_message_type()
        {
            var replyPipeline = new FakePipeline<IOutgoingReplyContext>();
            var context = CreateContext(replyPipeline);

            MessageOperations.Reply(context,new MyMessage(), new ReplyOptions());

            Assert.That(replyPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }


        [Test]
        public void When_publishing_event_class_should_set_class_as_message_type()
        {
            var publishPipeline = new FakePipeline<IOutgoingPublishContext>();
            var context = CreateContext(publishPipeline);

            MessageOperations.Publish(context,new MyMessage(), new PublishOptions());

            Assert.That(publishPipeline.ReceivedContext.Message.MessageType, Is.EqualTo(typeof(MyMessage)));
        }

        IBehaviorContext CreateContext<TContext>(IPipeline<TContext> pipeline) where TContext : IBehaviorContext
        {
            var pipelineCache = new FakePipelineCache();
            pipelineCache.RegisterPipeline(pipeline);

            var context = new TestableMessageHandlerContext();
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