namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.ObjectBuilder.Common;

    class CommonObjectBuilder : CommonObjectChildBuilder, IConfigureComponents, IBuilder
    {
        public IComponentConfig ConfigureComponent(Type concreteComponent, DependencyLifecycle instanceLifecycle)
        {
            ((IContainer)Container).Configure(concreteComponent, instanceLifecycle);

            return new ComponentConfig(concreteComponent, ((IContainer)Container));
        }

        public IComponentConfig<T> ConfigureComponent<T>(DependencyLifecycle instanceLifecycle)
        {
            ((IContainer)Container).Configure(typeof(T), instanceLifecycle);

            return new ComponentConfig<T>(((IContainer)Container));
        }

        public IComponentConfig<T> ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            ((IContainer)Container).Configure(componentFactory, instanceLifecycle);

            return new ComponentConfig<T>(((IContainer)Container));
        }

        public IComponentConfig<T> ConfigureComponent<T>(Func<IChildBuilder, T> componentFactory, DependencyLifecycle instanceLifecycle)
        {
            ((IContainer)Container).Configure(() => componentFactory(this), instanceLifecycle);

            return new ComponentConfig<T>(((IContainer)Container));
        }

        public IConfigureComponents ConfigureProperty<T>(Expression<Func<T, object>> property, object value)
        {
            var prop = Reflect<T>.GetProperty(property);

            return ((IConfigureComponents)this).ConfigureProperty<T>(prop.Name, value);
        }

        public IConfigureComponents ConfigureProperty<T>(string propertyName, object value)
        {
            ((IContainer)Container).ConfigureProperty(typeof(T), propertyName, value);

            return this;
        }

        IConfigureComponents IConfigureComponents.RegisterSingleton(Type lookupType, object instance)
        {
            ((IContainer)Container).RegisterSingleton(lookupType, instance);
            return this;
        }

        public IConfigureComponents RegisterSingleton<T>(T instance)
        {
            ((IContainer)Container).RegisterSingleton(typeof(T), instance);
            return this;
        }

        public IChildBuilder CreateChildBuilder()
        {
            return new CommonObjectBuilder
            {
                Container = ((IContainer)Container).BuildChildContainer()
            };
        }
    }
}
