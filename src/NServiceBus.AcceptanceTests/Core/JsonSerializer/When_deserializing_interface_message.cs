namespace NServiceBus.AcceptanceTests.Core.JsonSerializer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_deserializing_interface_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_blow_up()
        {
            var context = await Scenario.Define<Context>()
               .WithEndpoint<Endpoint>(c => c
                   .When(b => b.SendLocal<IMyMessage>(_ => { }))
                   .DoNotFailOnErrorMessages())
               .Done(c => c.FailedMessages.Any())
               .Run();

            var failedMessage = context.FailedMessages.SingleOrDefault();

            Assert.NotNull(failedMessage);

            var exception = failedMessage.Value.First().Exception;

            Assert.IsInstanceOf<MessageDeserializationException>(exception);
            StringAssert.Contains("interface", exception.ToString());
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseSerialization<SystemJsonSerializer>();
                });
            }

            class MyHandler : IHandleMessages<IMyMessage>
            {
                public Task Handle(IMyMessage message, IMessageHandlerContext context) => throw new NotImplementedException();
            }
        }

        public interface IMyMessage
        {
        }
    }
}