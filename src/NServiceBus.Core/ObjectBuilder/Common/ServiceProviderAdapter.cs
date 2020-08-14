namespace NServiceBus.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Janitor;
    using Microsoft.Extensions.DependencyInjection;
    using ObjectBuilder;

    [SkipWeaving]
    class ServiceProviderAdapter : IBuilder
    {
        public ServiceProviderAdapter(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public object Build(Type typeToBuild)
        {
            return serviceProvider.GetService(typeToBuild) ?? throw new Exception($"Unable to build {typeToBuild.FullName}. Ensure the type has been registered correctly with the container.");
        }

        public T Build<T>()
        {
            return (T)Build(typeof(T));
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return (IEnumerable<T>)BuildAll(typeof(T));
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return serviceProvider.GetServices(typeToBuild);
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            action(Build(typeToBuild));
        }

        public IBuilder CreateChildBuilder()
        {
            return new ChildScopeAdapter(serviceProvider.CreateScope());
        }

        public void Dispose()
        {
            //no-op
        }

        public void Release(object instance)
        {
            //no-op
        }

        readonly IServiceProvider serviceProvider;

        [SkipWeaving]
        class ChildScopeAdapter : IBuilder
        {
            public ChildScopeAdapter(IServiceScope serviceScope)
            {
                this.serviceScope = serviceScope;
            }

            public object Build(Type typeToBuild)
            {
                return serviceScope.ServiceProvider.GetService(typeToBuild) ?? throw new Exception($"Unable to build {typeToBuild.FullName}. Ensure the type has been registered correctly with the container.");
            }

            public T Build<T>()
            {
                return (T)Build(typeof(T));
            }

            public IEnumerable<T> BuildAll<T>()
            {
                return (IEnumerable<T>)BuildAll(typeof(T));
            }

            public IEnumerable<object> BuildAll(Type typeToBuild)
            {
                return serviceScope.ServiceProvider.GetServices(typeToBuild);
            }

            public void BuildAndDispatch(Type typeToBuild, Action<object> action)
            {
                action(Build(typeToBuild));
            }

            public IBuilder CreateChildBuilder()
            {
                throw new InvalidOperationException();
            }

            public void Dispose()
            {
                serviceScope.Dispose();
            }

            public void Release(object instance)
            {
                //no-op
            }

            readonly IServiceScope serviceScope;

            public object GetService(Type serviceType)
            {
                return Build(serviceType);
            }
        }

        public object GetService(Type serviceType)
        {
            return Build(serviceType);
        }
    }
}