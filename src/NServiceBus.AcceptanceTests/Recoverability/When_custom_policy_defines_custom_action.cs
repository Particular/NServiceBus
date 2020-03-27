namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_custom_policy_defines_custom_action : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_execute_custom_action_and_consume_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithCustomRecoverabilityAction>(e => e
                    .When(s => s.SendLocal(new FailingMessage())))
                .Done(ctx => ctx.CustomActionInvoked)
                .Run();

            Assert.IsTrue(context.CustomActionInvoked);
            Assert.AreEqual(1, context.HandlerInvocations);
            Assert.IsEmpty(context.FailedMessages);
        }

        class Context : ScenarioContext
        {
            public bool CustomActionInvoked { get; set; }
            public int HandlerInvocations { get; set; }
        }

        class EndpointWithCustomRecoverabilityAction : EndpointConfigurationBuilder
        {
            public EndpointWithCustomRecoverabilityAction()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.Pipeline.Register<FailingBehavior.Registration>();
                    c.Recoverability().CustomPolicy((config, context) => 
                        RecoverabilityAction.Custom(errorContext =>
                        {
                            var testContext = r.ScenarioContext as Context;
                            testContext.CustomActionInvoked = true;
                            return Task.FromResult(0);
                        }));
                });
            }

            // We need to throw the exception before the CaptureExceptionBehavior inserted by the testing framework.
            // The testing framework requires a detected failed message to be successfully retried or moved to the error queue.
            // This behavior is injected into the pipeline before this detection mechanism.
            public class FailingBehavior : Behavior<ITransportReceiveContext>
            {
                Context testContext;

                public FailingBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override Task Invoke(ITransportReceiveContext context, Func<Task> next)
                {
                    testContext.HandlerInvocations++;
                    throw new SimulatedException("message handler failure");
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base("FailingBehavior", typeof(FailingBehavior), "Injects a failing behavior into the pipeline")
                    {
                        InsertBefore("CaptureExceptionBehavior");
                    }
                }
            }
        }

        class FailingMessage : IMessage
        {
        }
    }
}