namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Threading;
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
                .Done(c => c.GotTheRequest)
                .Run();

            Assert.True(context.GotExceptionFromReply);

            //this verifies a callbacks assumption that core won't throw until after the `IOutgoingLogicalMessageContext` stage
            // See https://github.com/Particular/NServiceBus.Callbacks/blob/develop/src/NServiceBus.Callbacks/Reply/SetCallbackResponseReturnCodeBehavior.cs#L7
            Assert.True(context.WasAbleToInterceptBeforeCoreThrows, "Callbacks needs to be able to intercept the pipeline before core throws");
        }

        public class Context : ScenarioContext
        {
            public bool GotTheRequest { get; set; }
            public bool GotExceptionFromReply { get; set; }
            public bool WasAbleToInterceptBeforeCoreThrows { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register(typeof(CallbacksAssumptionVerifier), "Makes sure that callbacks can allow int replies"));
            }

            public class StartMessageHandler : IHandleMessages<MyRequest>
            {
                public StartMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

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

                    testContext.GotTheRequest = true;
                }

                Context testContext;
            }

            class CallbacksAssumptionVerifier : Behavior<IOutgoingLogicalMessageContext>
            {
                public CallbacksAssumptionVerifier(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next, CancellationToken token)
                {
                    testContext.WasAbleToInterceptBeforeCoreThrows = true;
                    return next();
                }

                Context testContext;
            }
        }

        public class MyRequest : IMessage
        {
        }
    }
}
