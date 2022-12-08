namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Recoverability;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<IInvokeHandlerContext>
    {
        public InvokeHandlerTerminator(IActivityFactory activityFactory)
        {
            this.activityFactory = activityFactory;
        }

        protected override async Task Terminate(IInvokeHandlerContext context)
        {
            if (context.Extensions.TryGet(out ActiveSagaInstance saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                return;
            }

            using var activity = activityFactory.StartHandlerActivity(context.MessageHandler, saga);

            var messageHandler = context.MessageHandler;

            // Might as well abort before invoking the handler if we're shutting down
            context.CancellationToken.ThrowIfCancellationRequested();

            var startTime = DateTimeOffset.UtcNow;
            try
            {
                await messageHandler
                    .Invoke(context.MessageBeingHandled, context)
                    .ThrowIfNull()
                    .ConfigureAwait(false);

                activity?.SetStatus(ActivityStatusCode.Ok);
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

                activity?.SetErrorStatus(ex);

                //context.Extensions.SetOnRoot(ex.GetHashCode(), messageHandler.HandlerType);

                if (messageHandler.Instance.GetType().GetInterface("IUseRecoverabilityConfiguration`1") is { } t)
                {
                    var configType = t.GetGenericArguments().Single();
                    var config = (IRecoverabilityConfiguration)Activator.CreateInstance(configType);
                    var recoverabilityConfiguration = config.OnError(ex, context.MessageBeingHandled, context.Headers, context.Extensions);
                    context.Extensions.SetOnRoot(ex.GetHashCode().ToString(), recoverabilityConfiguration);
                }

                throw;
            }
        }

        readonly IActivityFactory activityFactory;
    }
}