namespace NServiceBus.Sagas
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RemoveIncomingHeadersBehavior : IBehavior<IncomingContext>
    {
        [ObsoleteEx(RemoveInVersion = "6.0")]
        public void Invoke(IncomingContext context, Action next)
        {
            // We need this for backwards compatibility because in v4.0.0 we still have this headers being sent as part of the message even if MessageIntent == MessageIntentEnum.Publish
            if (context.PhysicalMessage.MessageIntent == MessageIntentEnum.Publish)
            {
                context.PhysicalMessage.Headers.Remove(Headers.SagaId);
                context.PhysicalMessage.Headers.Remove(Headers.SagaType);
            }

            next();
        }
    }
}