namespace NServiceBus.AcceptanceTests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_message_processing_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_provide_additional_exception_information()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFailingHandler>(e => e
                    .When(s => s.SendLocal(new FailingMessage()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.MessageFailed)
                .Run(TimeSpan.FromSeconds(10));

            Assert.IsTrue(context.Exception.Data.Contains("Message type"));
            Assert.IsTrue(context.Exception.Data.Contains("Handler type"));
            Assert.IsTrue(context.Exception.Data.Contains("Handler start time"));
            Assert.IsTrue(context.Exception.Data.Contains("Handler failure time"));
            Assert.IsTrue(context.Exception.Data.Contains("Message ID"));
            // we can't assert for the native message ID as not every transport has uses a different ID internally
        }

        class Context : ScenarioContext
        {
            public bool MessageFailed { get; set; }
            public Exception Exception { get; set; }
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.Notifications.Errors.MessageSentToErrorQueue += (sender, message) =>
                    {
                        var testContext = ((Context)r.ScenarioContext);
                        testContext.MessageFailed = true;
                        testContext.Exception = message.Exception;
                    };
                });
            }

            class FailingMessageHandler : IHandleMessages<FailingMessage>
            {
                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        public class FailingMessage : IMessage
        {
            
        }
    }
}