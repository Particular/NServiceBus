namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using NUnit.Framework;

    public class When_mixing_externally_and_internally_managed_containers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var container = new AcceptanceTestingContainer();
                var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

                endpointConfiguration.UseContainer(container);

                EndpointWithExternallyManagedContainer.Create(endpointConfiguration, new RegistrationPhaseAdapter(container));
            });
        }
    }
}