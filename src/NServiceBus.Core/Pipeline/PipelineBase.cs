namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using ObjectBuilder;

    [SkipWeaving]
    class PipelineBase<T> : IPipelineBase<T>
        where T : IBehaviorContext
    {
        public PipelineBase(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications)
        {
            var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals, pipelineModifications.Replacements);
          
            foreach (var rego in pipelineModifications.Additions.Where(x => x.IsEnabled(settings)))
            {
                coordinator.Register(rego);
            }

            behaviors = coordinator.BuildPipelineModelFor<T>()
                .Select(r => r.CreateBehavior(builder)).ToArray();
        }
        
        public Task Invoke(T context)
        {
            var pipeline = new BehaviorChain(behaviors);
            return pipeline.Invoke(context);
        }

        BehaviorInstance[] behaviors;
    }
}
