namespace NServiceBus.Pipeline
{
    class PipelineOverrideConfigurer : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.ForAllTypes<IPipelineOverride>(s => Configure.Instance.Configurer.ConfigureComponent(s, DependencyLifecycle.SingleInstance));
        }
    }
}