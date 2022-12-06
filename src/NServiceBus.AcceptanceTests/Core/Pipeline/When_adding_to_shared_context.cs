namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_adding_to_shared_context : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_available_on_error_context()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(b => b.DoNotFailOnErrorMessages()
                .When(session => session.SendLocal(new SomeMessage())))
                .Done(c => c.RecoverabilityExecuted)
                .Run();

            Assert.AreEqual(true, context.TheAlohaValue);
        }

        public class Context : ScenarioContext
        {
            public bool RecoverabilityExecuted { get; set; }
            public bool TheAlohaValue { get; set; }
        }

        public class SomeMessage : IMessage
        {
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>((endpointConfiguration, descriptor) =>
                {
                    endpointConfiguration.Recoverability().CustomPolicy(
                        (config, context) =>
                        {
                            var theAlohaValue = context.Extensions.GetRootContext().Get<bool>("ALOHA");

                            var testCtx = (Context)descriptor.ScenarioContext;
                            testCtx.RecoverabilityExecuted = true;
                            testCtx.TheAlohaValue = theAlohaValue;

                            return RecoverabilityAction.MoveToError("because");
                        });
                });
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    var sharedContext = context.Extensions.GetRootContext();
                    sharedContext.Set("ALOHA", true);

                    throw new InvalidOperationException("Something failed on purpose");
                }
            }
        }
    }
}
