
namespace NServiceBus.AcceptanceTests.Correlation
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_using_a_custom_correlation_id : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_the_given_id_as_the_transport_level_correlation_id()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<CorrelationEndpoint>(b => b.When(session =>
                {
                    var options = new SendOptions();

#pragma warning disable 618
                    options.SetCorrelationId(CorrelationId);
#pragma warning restore 618

                    options.RouteToThisEndpoint();

                    return session.Send(new MessageWithCustomCorrelationId(), options);
                }))
                .Done(c => c.GotRequest)
                .Run();

            Assert.AreEqual(CorrelationId, context.CorrelationIdReceived, "Correlation ids should match");
        }

        static string CorrelationId = "my_custom_correlation_id";

        public class Context : ScenarioContext
        {
            public bool GotRequest { get; set; }

            public string CorrelationIdReceived { get; set; }
        }

        public class CorrelationEndpoint : EndpointConfigurationBuilder
        {
            public CorrelationEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SendMessageWithCorrelationHandler : IHandleMessages<MessageWithCustomCorrelationId>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageWithCustomCorrelationId message, IMessageHandlerContext context)
                {
                    TestContext.CorrelationIdReceived = context.MessageHeaders[Headers.CorrelationId];

                    TestContext.GotRequest = true;

                    return Task.FromResult(0);
                }
            }
        }


        public class MessageWithCustomCorrelationId : IMessage
        {
        }
    }
}