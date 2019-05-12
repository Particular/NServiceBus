namespace NServiceBus
{
    using Extensibility;
    using ObjectBuilder;
    using Settings;

    class PipelineComponent
    {
        public PipelineComponent(SettingsHolder settings, IBuilder builder)
        {
            var pipelineCache = new PipelineCache(builder, settings.Get<PipelineConfiguration>());

            rootContextExtensions.Set<IPipelineCache>(pipelineCache);
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

        readonly ContextBag rootContextExtensions = new ContextBag();
    }
}