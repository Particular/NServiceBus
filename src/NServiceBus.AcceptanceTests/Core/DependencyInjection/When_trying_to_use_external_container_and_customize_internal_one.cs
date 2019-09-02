namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_trying_to_use_external_container_and_customize_internal_one : NServiceBusAcceptanceTest
    {
        [Test]
        public void It_throws_exception_when_preparing()
        {  
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<ExternalContainerEndpoint>(b => b.CustomConfig(c =>
                    {
                        c.UseContainer(new AcceptanceTestingContainer());
                    }))
                    .Done(c => c.Message != null)
                    .Run();
            });
        }

        class Context : ScenarioContext
        {
            public string Message { get; set; }
        }

        public class ExternalContainerEndpoint : EndpointConfigurationBuilder
        {
            public ExternalContainerEndpoint()
            {
                EndpointSetup<ExternalContainerServer>()
                    .ExternalContainer(new AcceptanceTestingContainer());
            }
        }
    }
}