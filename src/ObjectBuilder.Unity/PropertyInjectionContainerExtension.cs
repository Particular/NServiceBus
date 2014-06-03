namespace NServiceBus.ObjectBuilder.Unity
{
    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.ObjectBuilder;

    class PropertyInjectionContainerExtension : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Context.Strategies.Add(new PropertyInjectionBuilderStrategy(Container), UnityBuildStage.Initialization);
        }
    }
}