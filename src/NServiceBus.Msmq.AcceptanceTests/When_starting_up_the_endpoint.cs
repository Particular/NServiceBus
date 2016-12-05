namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using EndpointTemplates;

    public class When_starting_up_the_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_warning_if_queue_is_configured_with_anon_and_everyone_permissions()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Endpoint>(b => b.When((session, c) => Task.FromResult(0)))
                .Run();

            var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
            var anonymous = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

            var logItem = context.Logs.FirstOrDefault(item => item.Message.Contains($"[{everyone}] and/or [{anonymous}]"));
            Assert.IsNotNull(logItem);
            StringAssert.Contains($"is running with [{everyone}] and/or [{anonymous}] permissions. Consider setting appropriate permissions, if required by the organization", logItem.Message);
        }

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            static bool initialized;
            public Endpoint()
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