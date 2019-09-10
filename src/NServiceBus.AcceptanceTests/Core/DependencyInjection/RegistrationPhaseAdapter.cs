namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using ObjectBuilder;
    using ObjectBuilder.Common;

    class RegistrationPhaseAdapter : IConfigureComponents
    {
        public RegistrationPhaseAdapter(IContainer container)
        {
            this.container = container;
        }

        public void ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(concreteComponent, instanceLifecycle);
        }

        public void ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            container.Configure(typeof(T), instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(componentFactory, instanceLifecycle);
        }

        public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            container.Configure(() => componentFactory(new ResolutionPhaseAdapter(container)), instanceLifecycle);
        }

        void IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            container.RegisterSingleton(lookupType, instance);
        }

        public void RegisterSingleton<T>(T instance)
        {
            container.RegisterSingleton(typeof(T), instance);
        }

        public bool HasComponent<T>()
        {
            return container.HasComponent(typeof(T));
        }

        public bool HasComponent(Type componentType)
        {
            return container.HasComponent(componentType);
        }

        IContainer container;
    }
}