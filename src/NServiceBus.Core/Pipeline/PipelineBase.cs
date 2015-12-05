namespace NServiceBus
{
    using System.Linq;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Unicast.Transport;
    using ObjectBuilder;

    [SkipWeaving]
    class PipelineBase<T> : IPipelineBase<T>
        where T : BehaviorContext
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

        public void Initialize(PipelineInfo pipelineInfo)
        {
            foreach (var behaviorInstance in behaviors)
            {
                behaviorInstance.Initialize(pipelineInfo);
            }
        }

        public async Task Warmup()
        {
            foreach (var result in behaviors.Select(x => x.Warmup()))
            {
                await result.ConfigureAwait(false);
            }
        }

        public async Task Cooldown()
        {
            foreach (var result in behaviors.Select(x => x.Cooldown()))
            {
                await result.ConfigureAwait(false);
            }
        }

        public Task Invoke(T context)
        {
            var pipeline = new BehaviorChain(behaviors);
            return pipeline.Invoke(context);
        }

        BehaviorInstance[] behaviors;
    }
}
