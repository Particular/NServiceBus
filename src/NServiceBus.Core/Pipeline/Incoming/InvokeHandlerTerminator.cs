namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<IInvokeHandlerContext>
    {
        protected override Task Terminate(IInvokeHandlerContext context)
        {
            ActiveSagaInstance saga;

            if (context.Extensions.TryGet(out saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                return TaskEx.CompletedTask;
            }

            var messageHandler = context.MessageHandler;

            return messageHandler
                .Invoke(context.MessageBeingHandled, context)
                .ThrowIfNull();
        }
    }
}