namespace NServiceBus.MessageMutator
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ApplyIncomingTransportMessageMutatorsBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                mutator.MutateIncoming(context.PhysicalMessage);
            }

            next();
        }
    }
}