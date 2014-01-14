namespace NServiceBus.Sagas
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AuditInvokedSagaBehavior : IBehavior<HandlerInvocationContext>
    {
        public void Invoke(HandlerInvocationContext context, Action next)
        {
            next();

            ActiveSagaInstance saga;

            if (!context.TryGet(out saga) || saga.NotFound)
            {
                return;
            }

            var audit = string.Format("{0}:{1}",saga.SagaType.FullName,saga.Instance.Entity.Id);

            string header;

            if (context.LogicalMessage.Headers.TryGetValue(Headers.InvokedSagas, out header))
            {
                context.LogicalMessage.Headers[Headers.InvokedSagas] += ";" + audit;
            }
            else
            {
                context.LogicalMessage.Headers[Headers.InvokedSagas] = audit;
            }
        }
    }
}