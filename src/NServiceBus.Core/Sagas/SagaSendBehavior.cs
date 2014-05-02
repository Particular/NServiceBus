namespace NServiceBus.Sagas
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SagaSendBehavior : IBehavior<SendLogicalMessageContext>
    {
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && !saga.NotFound)
            {
                context.MessageToSend.Headers[Headers.OriginatingSagaId] = saga.Instance.Entity.Id.ToString();
                context.MessageToSend.Headers[Headers.OriginatingSagaType] = saga.SagaType.AssemblyQualifiedName;

            }

            //auto correlate with the saga we are replying to if needed
            if (context.SendOptions.Intent == MessageIntentEnum.Reply && context.IncomingMessage != null)
            {
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