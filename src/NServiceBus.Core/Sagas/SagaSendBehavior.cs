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

            next();
        }
    }
}