namespace NServiceBus.Sagas
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Transport;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SagaSendBehavior : IBehavior<SendLogicalMessageContext>
    {
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            if (context.LogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && !saga.NotFound)
            {
                context.LogicalMessage.Headers[Headers.OriginatingSagaId] = saga.Instance.Entity.Id.ToString();
                context.LogicalMessage.Headers[Headers.OriginatingSagaType] = saga.SagaType.AssemblyQualifiedName;
            }

            //auto correlate with the saga we are replying to if needed
            if (context.SendOptions.Intent == MessageIntentEnum.Reply && context.IncomingMessage != null)
            {
                string sagaId;
                string sagaType;

                if (context.IncomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out sagaId))
                {
                    context.LogicalMessage.Headers[Headers.SagaId] = sagaId;
                }

                if (context.IncomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out sagaType))
                {
                    context.LogicalMessage.Headers[Headers.SagaType] = sagaType;
                }
            }

            next();
        }
    }
}