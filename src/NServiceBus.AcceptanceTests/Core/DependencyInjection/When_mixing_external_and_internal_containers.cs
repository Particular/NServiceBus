namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using NServiceBus.ObjectBuilder;
    using NUnit.Framework;

    public class When_mixing_external_and_internal_containers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_prepare()
        {  
            Assert.Throws<InvalidOperationException>(() =>
            {
                var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

                endpointConfiguration.UseContainer(new AcceptanceTestingContainer());

                Endpoint.Prepare(endpointConfiguration, new ExternalContainer());
            });
        }

        class ExternalContainer : IConfigureComponents
        {
            public void ConfigureComponent(Type concreteComponent, DependencyLifecycle dependencyLifecycle)
            {
                throw new NotImplementedException();
            }

            public void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle)
            {
                throw new NotImplementedException();
            }

            public void ConfigureComponent<T>(Func<T> componentFactory, DependencyLifecycle dependencyLifecycle)
            {
                throw new NotImplementedException();
            }

            public void ConfigureComponent<T>(Func<IBuilder, T> componentFactory, DependencyLifecycle dependencyLifecycle)
            {
                throw new NotImplementedException();
            }

            public bool HasComponent<T>()
            {
                throw new NotImplementedException();
            }

            public bool HasComponent(Type componentType)
            {
                throw new NotImplementedException();
            }

            public void RegisterSingleton(Type lookupType, object instance)
            {
                throw new NotImplementedException();
            }

            public void RegisterSingleton<T>(T instance)
            {
                throw new NotImplementedException();
            }
        }
    }
}