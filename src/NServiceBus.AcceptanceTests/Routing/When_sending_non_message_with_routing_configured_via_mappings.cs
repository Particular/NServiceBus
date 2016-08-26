namespace NServiceBus.AcceptanceTests.BestPractices
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_non_message_with_routing_configured_via_mappings : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_when_sending()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .WithEndpoint<Sender>(b => b.When(async (session, c) =>
                {
                    try
                    {
                        await session.Send(new NonMessage());
                    }
                    catch (Exception ex)
                    {
                        c.Exception = ex;
                        c.GotTheException = true;
                    }
                }))
                .Done(c => c.GotTheException)
                .Run();

            StringAssert.Contains("No destination specified for messag", context.Exception.ToString());
        }

        public class Context : ScenarioContext
        {
            public bool GotTheException { get; set; }
            public Exception Exception { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<NonMessage>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class NonMessage
        {
        }
    }
}