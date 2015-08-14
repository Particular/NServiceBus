namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline.Contexts;
    using Saga;

    class InvokeHandlersBehavior : HandlingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && saga.NotFound && saga.SagaType == context.MessageHandler.Instance.GetType())
            {
                await next().ConfigureAwait(false);
                return;
            }

            var messageHandler = context.MessageHandler;

            messageHandler.Invocation(messageHandler.Instance, context.MessageBeingHandled);
            await next().ConfigureAwait(false);
        }
    }
}