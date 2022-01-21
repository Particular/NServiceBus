namespace NServiceBus.AcceptanceTests.Core.Recoverability
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_message_is_dispatched_to_error_queue : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_allow_body_to_be_manipulated()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFailingHandler>(b => b
                    .DoNotFailOnErrorMessages()
                    .When((session, ctx) => session.SendLocal(new InitiatingMessage()))
                )
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.True(context.MessageBodyWasEmpty);
        }

        class Context : ScenarioContext
        {
            public bool MessageMovedToErrorQueue { get; set; }
            public bool MessageBodyWasEmpty { get; internal set; }
        }

        class EndpointWithFailingHandler : EndpointConfigurationBuilder
        {
            static string errorQueueAddress = Conventions.EndpointNamingConvention(typeof(ErrorSpy));

            public EndpointWithFailingHandler()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.SendFailedMessagesTo(errorQueueAddress);
                    config.Pipeline.Register(typeof(ErrorBodyStorageBehavior), "Simulate writing the body to a separate storage and pass a null body to the transport");
                });
            }

            public class ErrorBodyStorageBehavior : Behavior<IDispatchContext>
            {
                public override Task Invoke(IDispatchContext context, Func<Task> next)
                {
                    foreach (var operation in context.Operations)
                    {
                        var unicastAddress = operation.AddressTag as UnicastAddressTag;

                        if (unicastAddress?.Destination != errorQueueAddress)
                        {
                            continue;
                        }

                        operation.Message.UpdateBody(null); //TODO: Would ReadOnlyMemory<byte>.Empty be better?
                    }
                    return next();
                }
            }

            class InitiatingHandler : IHandleMessages<InitiatingMessage>
            {
                public Task Handle(InitiatingMessage initiatingMessage, IMessageHandlerContext context)
                {
                    throw new SimulatedException("Some failure");
                }
            }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register(typeof(ErrorMessageDetector), "Detect incoming error messages"));
            }

            class ErrorMessageDetector : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
            {
                public ErrorMessageDetector(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
                {
                    testContext.MessageBodyWasEmpty = context.Message.Body.IsEmpty;
                    testContext.MessageMovedToErrorQueue = true;
                    return next(context);
                }

                Context testContext;
            }
        }

        public class InitiatingMessage : IMessage
        {
        }
    }
}