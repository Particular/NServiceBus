namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<IInvokeHandlerContext>
    {
        protected override async Task Terminate(IInvokeHandlerContext context)
        {
            if (context.Extensions.TryGet(out ActiveSagaInstance saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                return;
            }

            var messageHandler = context.MessageHandler;

            var startTimestamp = DateTime.UtcNow;
            try
            {
                await messageHandler
                    .Invoke(context.MessageBeingHandled, context)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.Data["Message type"] = context.MessageMetadata.MessageType.FullName;
                e.Data["Handler type"] = context.MessageHandler.HandlerType;
                e.Data["Handler start time"] = startTimestamp;
                e.Data["Handler failure time"] = DateTime.UtcNow;
                throw;
            }
        }
    }
}