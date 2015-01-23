namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;


    class PipelineBase<T>where T:BehaviorContext
    {
        public PipelineBase(IBuilder builder, PipelineModifications pipelineModifications)
        {
            busNotifications = builder.Build<BusNotifications>();
            contextStacker = builder.Build<BehaviorContextStacker>();

            var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals, pipelineModifications.Replacements);
            foreach (var rego in pipelineModifications.Additions)
            {
                coordinator.Register(rego);
            }

            steps = coordinator.BuildPipelineModelFor<T>();

            behaviors = steps.Select(r => r.CreateBehavior(builder)).ToArray();
        }

        public  BehaviorContext Invoke(T context)
        {
            var lookupSteps = steps.ToDictionary(rs => rs.BehaviorType, ss => ss.StepId);
            var pipeline = new BehaviorChain(behaviors, context, lookupSteps, busNotifications);
            return pipeline.Invoke(contextStacker);
        }



        BehaviorInstance[] behaviors;
        BusNotifications busNotifications;
        BehaviorContextStacker contextStacker; IList<RegisterStep> steps;
    }
}
