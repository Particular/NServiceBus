namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;


    class ApplyIncomingTransportMessageMutatorsBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override Task Invoke(Context context, Func<Task> next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                mutator.MutateIncoming(context.GetPhysicalMessage());
            }

            return next();
        }
    }
}