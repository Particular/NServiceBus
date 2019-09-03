namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using ObjectBuilder;

    class FakeExternalContainer : IConfigureComponents
    {
        public void ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
        {
        }

        public void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle)
        {
        }

        public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
        }

        public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle dependencyLifecycle)
        {
        }

        public bool HasComponent<T>()
        {
            return false;
        }

        public bool HasComponent(Type componentType)
        {
            return false;
        }

        public void RegisterSingleton(Type lookupType, object instance)
        {
        }

        public void RegisterSingleton<T>(T instance)
        {
        }
    }
}