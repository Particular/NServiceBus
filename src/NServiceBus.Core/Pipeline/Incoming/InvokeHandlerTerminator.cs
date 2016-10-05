namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<IInvokeHandlerContext>
    {
        protected override Task Terminate(IInvokeHandlerContext context)
        {
            context.Extensions.Set(new State
            {
                ScopeWasPresent = Transaction.Current != null
            });

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

        public class State
        {
            public bool ScopeWasPresent { get; set; }
        }
    }
}