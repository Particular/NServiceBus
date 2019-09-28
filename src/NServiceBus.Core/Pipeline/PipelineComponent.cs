namespace NServiceBus
{
    using Extensibility;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using System.Threading.Tasks;

    class PipelineComponent
    {
        public PipelineComponent(SettingsHolder settings)
        {
            modifications = new PipelineModifications();
            PipelineSettings = new PipelineSettings(modifications, settings);
        }

        public Task Start()
        {
            rootContextExtensions.Set<IPipelineCache>(new PipelineCache(container.Builder, modifications));
            return Task.FromResult(0);
        }

        public void AddRootContextItem<T>(T item)
        {
            rootContextExtensions.Set(item);
        }

        public RootContext CreateRootContext(IBuilder scopedBuilder, ContextBag extensions = null)
        {
            var context = new RootContext(scopedBuilder);

            context.Extensions.Merge(rootContextExtensions);

            if (extensions != null)
            {
                context.Extensions.Merge(extensions);
            }

            return context;
        }

        public PipelineSettings PipelineSettings { get; }

        public void Initialize(ContainerComponent containerComponent)
        {
            container = containerComponent;
            foreach (var registeredBehavior in modifications.Replacements)
            {
                container.ContainerConfiguration.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in modifications.Additions)
            {
                step.ApplyContainerRegistration(container.ContainerConfiguration);
            }
        }

        readonly PipelineModifications modifications;
        readonly ContextBag rootContextExtensions = new ContextBag();
        ContainerComponent container;
    }
}