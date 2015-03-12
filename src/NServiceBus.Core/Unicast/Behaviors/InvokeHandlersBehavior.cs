namespace NServiceBus
{
    using System;
    using Pipeline.Contexts;
    using Sagas;

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

            messageHandler.Invoke(context.IncomingLogicalMessage.Instance, null /* Daniel: Not done yet*/);
            next();
        }
    }
}