namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_starting_up_the_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_log_warning_if_queue_is_configured_with_anon_and_everyone_permissions()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };
            Scenario.Define(context)
                .WithEndpoint<EndPoint>(b => b.Given((bus, c) => { }))
                .Run();

            var logItem = context.Logs.FirstOrDefault(item => item.Message.Contains(@"[Everyone] and [NT AUTHORITY\ANONYMOUS LOGON]"));
            Assert.IsNotNull(logItem);
            StringAssert.Contains(@"is running with [Everyone] and [NT AUTHORITY\ANONYMOUS LOGON] permissions. Consider setting appropriate permissions, if required by your organization", logItem.Message);
        }

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
        }

        class EndPoint : EndpointConfigurationBuilder
        {
            static bool initialized;
            public EndPoint()
            {
                if (initialized)
                {
                    return;
                }
                initialized = true;
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>();
                });
            }
        }
    }
}