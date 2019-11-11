namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using ObjectBuilder;
    using Pipeline;

    class PipelineComponent
    {
        public PipelineComponent(PipelineModifications modifications)
        {
            this.modifications = modifications;
        }

        public static PipelineComponent Initialize(PipelineSettings settings, ContainerComponent containerComponent)
        {
            var modifications = settings.modifications;

            foreach (var registeredBehavior in modifications.Replacements)
            {
                containerComponent.ContainerConfiguration.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in modifications.Additions)
            {
                step.ApplyContainerRegistration(containerComponent.ContainerConfiguration);
            }

            return new PipelineComponent(modifications);
        }

        public Task Start(IBuilder builder)
        {
            rootContextExtensions.Set<IPipelineCache>(new PipelineCache(builder, modifications));
            return Task.FromResult(0);
        }

        public void AddRootContextItem<T>(T item)
        {
            rootContextExtensions.Set(item);
        }

        public RootContext CreateRootContext(IBuilder builder, ContextBag extensions = null)
        {
            var context = new RootContext(builder);

            context.Extensions.Merge(rootContextExtensions);

            if (extensions != null)
            {
                context.Extensions.Merge(extensions);
            }

            return context;
        }

        readonly PipelineModifications modifications;
        readonly ContextBag rootContextExtensions = new ContextBag();
    }
}