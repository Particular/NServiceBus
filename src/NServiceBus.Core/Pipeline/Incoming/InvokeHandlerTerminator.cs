﻿namespace NServiceBus
{
    using System;
    using System.Threading;
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

            var startTime = DateTimeOffset.UtcNow;
            try
            {
                await messageHandler
                    // TODO: CancellationToken.None is temporary
                    .Invoke(context.MessageBeingHandled, context, CancellationToken.None)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.Data["Message type"] = context.MessageMetadata.MessageType.FullName;
                e.Data["Handler type"] = context.MessageHandler.HandlerType.FullName;
                e.Data["Handler start time"] = DateTimeOffsetHelper.ToWireFormattedString(startTime);
                e.Data["Handler failure time"] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow);
                throw;
            }
        }
    }
}