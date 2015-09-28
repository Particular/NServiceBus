namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline.Contexts;
    using Sagas;

    class InvokeHandlersBehavior : HandlingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            context.Set(new State { ScopeWasPresent = Transaction.Current != null });

            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && saga.NotFound && saga.Metadata.SagaType == context.MessageHandler.Instance.GetType())
            {
                await next().ConfigureAwait(false);
                return;
            }

            var messageHandler = context.MessageHandler;
            await messageHandler.Invoke(context.MessageBeingHandled).ConfigureAwait(false);

            await next().ConfigureAwait(false);
        }
        public class State
        {
            public bool ScopeWasPresent { get; set; }
        }
    }
}