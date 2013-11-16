namespace NServiceBus.Sagas
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class SagaSendBehavior : IBehavior<SendLogicalMessageContext>
    {
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            ActiveSagaInstance saga;

            if (context.TryGet(out saga))
            {
                context.MessageToSend.Headers[Headers.OriginatingSagaId] = saga.Instance.Entity.Id.ToString();
                context.MessageToSend.Headers[Headers.OriginatingSagaType] = saga.SagaType.AssemblyQualifiedName;

            }

            //auto correlate with the saga we are replying to if needed
            if (context.SendOptions.Intent == MessageIntentEnum.Reply && context.IncomingMessage != null)
            {
                //for now we revert back to send since this would be a breaking change. We'll fix this in v4.1
                //https://github.com/NServiceBus/NServiceBus/issues/1409
                context.SendOptions.Intent = MessageIntentEnum.Send;

                string sagaId;
                string sagaType;

                if (context.IncomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out sagaId))
                {
                    context.MessageToSend.Headers[Headers.SagaId] = sagaId;
                }

                if (context.IncomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out sagaType))
                {
                    context.MessageToSend.Headers[Headers.SagaType] = sagaType;
                }
            }

            next();
        }
    }
}