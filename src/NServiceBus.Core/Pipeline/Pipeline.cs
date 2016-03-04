namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using Janitor;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

    [SkipWeaving]
    class Pipeline<TContext> : IPipeline<TContext>
        where TContext : IBehaviorContext
    {
        public Pipeline(IBuilder builder, ReadOnlySettings settings, PipelineModifications pipelineModifications)
        {
            var coordinator = new StepRegistrationsCoordinator(pipelineModifications.Removals, pipelineModifications.Replacements);

            foreach (var rego in pipelineModifications.Additions.Where(x => x.IsEnabled(settings)))
            {
                coordinator.Register(rego);
            }

            behaviors = coordinator.BuildPipelineModelFor<TContext>()
                .Select(r => r.CreateBehavior(builder)).ToArray();
        }

        public Task Invoke(TContext context)
        {
            var pipeline = new BehaviorChain(behaviors);
            return pipeline.Invoke(context);
        }

        BehaviorInstance[] behaviors;
    }
}