namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using AcceptanceTesting.Customization;
    using NServiceBus.Config;
    using Faults;
    using NServiceBus.Support;
    using Timeout.Core;
    using NUnit.Framework;

    class When_second_level_retry_satellite_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public void Message_sent_to_error_queue_should_have_faildq_set_to_main_queue()
        {
            var context = new TestContext();
            Scenario.Define(context)
                .AllowExceptions(ex => ex.Message.Contains("Simulated!"))
                .WithEndpoint<ErrorSpyEndpoint>()
                .WithEndpoint<EndpointWithFaultySLR>(c =>
                {
                    c.When(b => b.SendLocal(new MyMessage()));
                })
                .Done(c => context.Done)
                .Run();

            var endpointAddress = $"{Conventions.EndpointNamingConvention(typeof(EndpointWithFaultySLR))}@{RuntimeEnvironment.MachineName}";
            Assert.AreEqual(endpointAddress, context.Headers[FaultsHeaderKeys.FailedQ]);
        }

        public class TestContext : ScenarioContext
        {
            public IDictionary<string, string> Headers { get; set; }
            public bool Done { get; set; }
        }

        public class MyMessage : IMessage
        {
        }

        public class EndpointWithFaultySLR : EndpointConfigurationBuilder
        {
            public TestContext TestContext { get; set; }

            public EndpointWithFaultySLR()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.SuppressOutdatedTimeoutPersistenceWarning();
                }).WithConfig<TransportConfig>(c =>
                {
                    c.MaxRetries = 0;
                }).WithConfig<UnicastBusConfig>(c =>
                {
                    c.TimeoutManagerAddress = "invalid-address";
                }).WithConfig<MessageForwardingInCaseOfFaultConfig>(c =>
                {
                    c.ErrorQueue = Conventions.EndpointNamingConvention(typeof(ErrorSpyEndpoint));
                });
            }

            public class Handler : IHandleMessages<MyMessage>
            {
                public void Handle(MyMessage message)
                {
                    throw new Exception("Simulated!");
                }
            }
        }

        class ErrorSpyEndpoint : EndpointConfigurationBuilder
        {
            public ErrorSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MyMessage>
            {
                public TestContext TestContext { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    TestContext.Headers = Bus.CurrentMessageContext.Headers;
                    TestContext.Done = true;
                }
            }
        }
    }
}
