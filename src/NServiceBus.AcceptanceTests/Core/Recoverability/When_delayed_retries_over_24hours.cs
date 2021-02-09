namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_delayed_retries_over_24hours : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_move_message_to_error()
        {
            Requires.DelayedDelivery();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<RetryEndpoint>(b => b
                    .When(session => session.SendLocal(new MessageToBeRetried()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.FailedMessages.Any())
                .Run(TimeSpan.FromSeconds(10));

            var failedMessage = context.FailedMessages.Values.First().First();

            Assert.AreEqual(1, int.Parse(failedMessage.Headers[Headers.DelayedRetries]));
        }

        class Context : ScenarioContext
        {
            public byte[] OriginalBody { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                 {
                     config.GetSettings().Set("Recoverablity.MaxDurationOfDelayedRetries", TimeSpan.FromSeconds(1));
                     var recoverability = config.Recoverability();
                     recoverability.Delayed(settings => settings.NumberOfRetries(2).TimeIncrease(TimeSpan.FromSeconds(1)));
                 });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    throw new SimulatedException();
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
        }
    }
}