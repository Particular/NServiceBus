namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using NUnit.Framework;
    using AcceptanceTesting.Customization;
    using System.Collections.Generic;

    public class When_trying_to_resolve_the_message_session_before_start : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

            endpointConfiguration.TypesToIncludeInScan(new List<Type>());

            endpointConfiguration.UseTransport<LearningTransport>();

            var preparedEndpoint = Endpoint.Prepare(endpointConfiguration, new FakeExternalContainer());

            Assert.Throws<InvalidOperationException>(() =>
            {
                preparedEndpoint.MessageSessionProvider();
            });
        }
    }
}