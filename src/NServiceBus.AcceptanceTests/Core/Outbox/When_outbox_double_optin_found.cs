namespace NServiceBus.AcceptanceTests.Core.Outbox
{
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Logging;
    using NUnit.Framework;

    public class When_outbox_double_optin_found : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_warning()
        {
            Requires.OutboxPersistence();

            ConfigurationManager.AppSettings["NServiceBus/Outbox"] = bool.TrueString;

            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<NonDtcReceivingEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            var logItem = context.Logs.FirstOrDefault(item => item.Message.Contains("The double opt-in to use the Outbox feature with") && item.Level == LogLevel.Warn);
            Assert.IsNotNull(logItem);
            StringAssert.AreEqualIgnoringCase(@"The double opt-in to use the Outbox feature with MSMQ or SQLServer transport is no longer required. It is safe to remove the following line:
    <add key=""NServiceBus/Outbox"" value=""true""/>
from your <appSettings /> section in the application configuration file.", logItem.Message);
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(b => { b.EnableOutbox(); });
            }
        }
    }
}