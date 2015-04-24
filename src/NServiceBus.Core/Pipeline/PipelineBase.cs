namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Settings;
    using NServiceBus.Unicast.Transport;
    using ObjectBuilder;


    class PipelineBase<T>where T:BehaviorContext
    {
        public PipelineBase(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications, RegisterStep receiveBehavior = null)
        {
            busNotifications = builder.Build<BusNotifications>();
            contextStacker = builder.Build<BehaviorContextStacker>();

            var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals, pipelineModifications.Replacements);
            if (receiveBehavior != null)
            {
                coordinator.Register(receiveBehavior);
            }
            foreach (var rego in pipelineModifications.Additions.Where(x => x.IsEnabled(settings)))
            {
                coordinator.Register(rego);
            }

            steps = coordinator.BuildPipelineModelFor<T>();

            behaviors = steps.Select(r => r.CreateBehavior(builder)).ToArray();
        }

        public void Initialize(PipelineInfo pipelineInfo)
        {
            foreach (var behaviorInstance in behaviors)
            {
                behaviorInstance.Initialize(pipelineInfo);
            }
        }

        public void OnStarting()
        {
            foreach (var behaviorInstance in behaviors)
            {
                behaviorInstance.OnStarting();
            }
        }

        public void OnStopped()
        {
            foreach (var behaviorInstance in behaviors)
            {
                behaviorInstance.OnStopped();
            }
        }

        public void Invoke(T context)
        {
            var lookupSteps = steps.ToDictionary(rs => rs.BehaviorType, ss => ss.StepId);
            var pipeline = new BehaviorChain(behaviors, context, lookupSteps, busNotifications);
            pipeline.Invoke(contextStacker);
        }

        BehaviorInstance[] behaviors;
        BusNotifications busNotifications;
        BehaviorContextStacker contextStacker; IList<RegisterStep> steps;
    }
}
