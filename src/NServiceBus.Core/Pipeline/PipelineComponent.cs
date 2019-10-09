namespace NServiceBus
{
    using System.Threading.Tasks;
    using Extensibility;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

    class PipelineComponent
    {
        public PipelineComponent(SettingsHolder settings)
        {
            modifications = new PipelineModifications();
            PipelineSettings = new PipelineSettings(modifications, settings, OnReceivePipelineCompleted);
        }

        public IEventNotification<ReceivePipelineCompleted> OnReceivePipelineCompleted => pipelineCompletedNotification;
        public MainPipelineExecutor PipelineExecutor { get; internal set; }

        public PipelineSettings PipelineSettings { get; }

        public Task Start()
        {
            rootContextExtensions.Set<IPipelineCache>(new PipelineCache(container.Builder, modifications));
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

            PipelineExecutor = new MainPipelineExecutor(containerComponent.Builder, CreateRootContext, pipelineCompletedNotification);
        }

        readonly PipelineModifications modifications;
        readonly ContextBag rootContextExtensions = new ContextBag();
        Notification<ReceivePipelineCompleted> pipelineCompletedNotification = new Notification<ReceivePipelineCompleted>();
        ContainerComponent container;
    }
}