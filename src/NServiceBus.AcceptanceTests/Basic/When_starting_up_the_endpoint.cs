namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_starting_up_the_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_warning_if_queue_is_configured_with_anon_and_everyone_permissions()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<EndPoint>(b => b.Given((bus, c) => Task.FromResult(0)))
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