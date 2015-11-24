namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_Audit_OverrideTimeToBeReceived_set_and_transactional_Msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public async void Endpoint_should_not_start_and_show_error()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.True(context.Exceptions.First().Message.Equals("Setting a custom OverrideTimeToBeReceived for audits is not supported on transactional MSMQ."));
        }
        
        public class Context : ScenarioContext { }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.Transactions().Enable();
                })
                .WithConfig<AuditConfig>(c => c.OverrideTimeToBeReceived = TimeSpan.FromHours(1));
            }
        }
    }
}