namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
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

                Endpoint.Prepare(endpointConfiguration, new FakeExternalContainer());
            });
        }
    }
}