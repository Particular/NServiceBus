namespace NServiceBus
{
    using Extensibility;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

    class PipelineComponent
    {
        public PipelineComponent(SettingsHolder settings)
        {
            modifications = new PipelineModifications();
            PipelineSettings = new PipelineSettings(modifications, settings);
        }

        public void InitializeBuilder(IBuilder builder)
        {
            rootContextExtensions.Set<IPipelineCache>(new PipelineCache(builder, modifications));
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

        public PipelineSettings PipelineSettings { get; }

        public void RegisterBehaviorsInContainer(IConfigureComponents container)
        {
            foreach (var registeredBehavior in modifications.Replacements)
            {
                container.ConfigureComponent(registeredBehavior.BehaviorType, DependencyLifecycle.InstancePerCall);
            }

            foreach (var step in modifications.Additions)
            {
                step.ApplyContainerRegistration(container);
            }
        }

        readonly PipelineModifications modifications;
        readonly ContextBag rootContextExtensions = new ContextBag();
    }
}