namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<IInvokeHandlerContext>
    {
        public InvokeHandlerTerminator(ActivityFactory activityFactory)
        {
            this.activityFactory = activityFactory;
        }

        protected override async Task Terminate(IInvokeHandlerContext context)
        {
            if (context.Extensions.TryGet(out ActiveSagaInstance saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                return;
            }

            using var activity = activityFactory?.StartHandlerActivity(context.MessageHandler, saga);

            var messageHandler = context.MessageHandler;

            // Might as well abort before invoking the handler if we're shutting down
            context.CancellationToken.ThrowIfCancellationRequested();

            var startTime = DateTimeOffset.UtcNow;
            try
            {
                await TracingHelper.TryTraceInvocation(activity, async value =>
                {
                    (MessageHandler handler, IInvokeHandlerContext ctx) = value;
                    await handler
                        .Invoke(ctx.MessageBeingHandled, ctx)
                        .ThrowIfNull()
                        .ConfigureAwait(false);
                }, (messageHandler, context)).ConfigureAwait(false);
            }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - enriching and rethrowing
            catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
            {
                ex.Data["Message type"] = context.MessageMetadata.MessageType.FullName;
                ex.Data["Handler type"] = context.MessageHandler.HandlerType.FullName;
                ex.Data["Handler start time"] = DateTimeOffsetHelper.ToWireFormattedString(startTime);
                ex.Data["Handler failure time"] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
                ex.Data["Handler canceled"] = context.CancellationToken.IsCancellationRequested;
                throw;
            }
        }

        readonly ActivityFactory activityFactory;
    }
}