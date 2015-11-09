namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Unicast;
    using Pipeline;
    using Pipeline.Contexts;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<InvokeHandlerContext>
    {
        protected override async Task Terminate(InvokeHandlerContext context)
        {
            context.Set(new State { ScopeWasPresent = Transaction.Current != null });

            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                return;
            }

            var messageHandler = context.MessageHandler;
            await messageHandler
                .Invoke(context.MessageBeingHandled, new MessageHandlerContext(context, busOperations))
                .ConfigureAwait(false);
        }
        public InvokeHandlerTerminator(BusOperations busOperations)
        {
            this.busOperations = busOperations;
        }

        BusOperations busOperations;

        public class State
        {
            public bool ScopeWasPresent { get; set; }
        }
    }
}