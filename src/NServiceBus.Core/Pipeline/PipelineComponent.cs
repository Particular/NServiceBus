namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

    class PipelineComponent
    {
        public PipelineComponent(SettingsHolder settings, IBuilder builder)
        {
            this.settings = settings;
            this.builder = builder;
        }

        public void Initialize()
        {
            pipelineCache = new PipelineCache(builder, settings.Get<PipelineConfiguration>());
        }

        public void AddRootContextItem<T>(object item)
        {
            rootContextItems.Add(typeof(T).FullName, item);
        }

        public async Task<TContext> Invoke<TContext>(IBuilder rootBuilder, Func<IBehaviorContext, TContext> contextFactory) where TContext : IBehaviorContext
        {
            var context = contextFactory(CreateRootContext(rootBuilder));

            await context.InvokePipeline().ConfigureAwait(false);

            return context;
        }

        public RootContext CreateRootContext(IBuilder rootBuilder)
        {
            var context = new RootContext(rootBuilder);

            context.Set(pipelineCache);

            foreach (var contextItem in rootContextItems)
            {
                context.Set(contextItem.Key, contextItem.Value);
            }

            return context;
        }

        IPipelineCache pipelineCache;

        readonly Dictionary<string, object> rootContextItems = new Dictionary<string, object>();
        readonly SettingsHolder settings;
        readonly IBuilder builder;
    }
}