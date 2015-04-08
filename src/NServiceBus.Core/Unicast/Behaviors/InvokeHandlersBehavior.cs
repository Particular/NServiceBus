namespace NServiceBus
{
    using System;
    using Pipeline.Contexts;
    using Saga;

    class InvokeHandlersBehavior : HandlingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && saga.NotFound && saga.SagaType == context.MessageHandler.HandlerType)
            {
                next();
                return;
            }

            var messageHandler = context.MessageHandler;
            var invocationContext = context.Get<object>("InvocationContext");
            messageHandler.Invoke(context.IncomingLogicalMessage.Instance, invocationContext);
            next();
        }
    }
}