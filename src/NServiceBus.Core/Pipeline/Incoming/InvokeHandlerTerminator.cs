﻿namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline;
    using Pipeline.Contexts;
    using Sagas;

    class InvokeHandlerTerminator : PipelineTerminator<InvokeHandlerContext>
    {
        protected override async Task Terminate(InvokeHandlerContext context)
        {
            context.Extensions.Set(new State { ScopeWasPresent = Transaction.Current != null });

            ActiveSagaInstance saga;

            if (context.Extensions.TryGet(out saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                return;
            }

            var messageHandler = context.MessageHandler;
            await messageHandler
                .Invoke(context.MessageBeingHandled, context)
                .ConfigureAwait(false);
        }

        public class State
        {
            public bool ScopeWasPresent { get; set; }
        }
    }
}