namespace NServiceBus.AcceptanceTests.Core.Routing;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_callbacks_are_used_to_reply_with_int_or_enum : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_throw_late_enough_to_allow_intercept()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(c => c.When(b => b.SendLocal(new MyRequest())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.GotExceptionFromReply, Is.True);

            //this verifies a callbacks assumption that core won't throw until after the `IOutgoingLogicalMessageContext` stage
            // See https://github.com/Particular/NServiceBus.Callbacks/blob/develop/src/NServiceBus.Callbacks/Reply/SetCallbackResponseReturnCodeBehavior.cs#L7
            Assert.That(context.WasAbleToInterceptBeforeCoreThrows, Is.True, "Callbacks needs to be able to intercept the pipeline before core throws");
        }
    }

    public class Context : ScenarioContext
    {
        public bool GotExceptionFromReply { get; set; }
        public bool WasAbleToInterceptBeforeCoreThrows { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>(c => c.Pipeline.Register(typeof(CallbacksAssumptionVerifier), "Makes sure that callbacks can allow int replies"));

        [Handler]
        public class StartMessageHandler(Context testContext) : IHandleMessages<MyRequest>
        {
            public async Task Handle(MyRequest message, IMessageHandlerContext context)
            {
                try
                {
                    await context.Reply(10);
                }
                catch (Exception)
                {
                    testContext.GotExceptionFromReply = true;
                }

                testContext.MarkAsCompleted();
            }
        }

        class CallbacksAssumptionVerifier(Context testContext) : Behavior<IOutgoingLogicalMessageContext>
        {
            public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                testContext.WasAbleToInterceptBeforeCoreThrows = true;
                return next();
            }
        }
    }

    public class MyRequest : IMessage;
}
