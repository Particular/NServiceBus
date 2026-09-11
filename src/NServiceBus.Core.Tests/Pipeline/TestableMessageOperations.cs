namespace NServiceBus.Core.Tests.Pipeline;

using System;
using NServiceBus.Pipeline;
using System.Threading.Tasks;
using MessageInterfaces.MessageMapper.Reflection;

class TestableMessageOperations : MessageOperations
{
    public Pipeline<IOutgoingPublishContext> PublishPipeline => (Pipeline<IOutgoingPublishContext>)((TracedPipeline<IOutgoingPublishContext>)publishPipeline).Inner;
    public Pipeline<IOutgoingSendContext> SendPipeline => (Pipeline<IOutgoingSendContext>)((TracedPipeline<IOutgoingSendContext>)sendPipeline).Inner;
    public Pipeline<IOutgoingReplyContext> ReplyPipeline => (Pipeline<IOutgoingReplyContext>)((TracedPipeline<IOutgoingReplyContext>)replyPipeline).Inner;
    public Pipeline<ISubscribeContext> SubscribePipeline => (Pipeline<ISubscribeContext>)((TracedPipeline<ISubscribeContext>)subscribePipeline).Inner;
    public Pipeline<IUnsubscribeContext> UnsubscribePipeline => (Pipeline<IUnsubscribeContext>)((TracedPipeline<IUnsubscribeContext>)unsubscribePipeline).Inner;

    public TestableMessageOperations() : base(new MessageMapper(), new Pipeline<IOutgoingPublishContext>(), new Pipeline<IOutgoingSendContext>(), new Pipeline<IOutgoingReplyContext>(), new Pipeline<ISubscribeContext>(), new Pipeline<IUnsubscribeContext>(), new ActivityFactory(new InstrumentationOptions()))
    {
    }

    public class Pipeline<TContext> : IPipeline<TContext> where TContext : IBehaviorContext
    {
        public Action<TContext> OnInvoke { get; set; }

        public TContext LastContext { get; private set; }

        public Task Invoke(TContext context)
        {
            LastContext = context;
            OnInvoke?.Invoke(context);

            return Task.CompletedTask;
        }
    }
}