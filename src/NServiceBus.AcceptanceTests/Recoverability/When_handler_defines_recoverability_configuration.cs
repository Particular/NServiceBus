namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Recoverability;
using NUnit.Framework;

public class When_handler_defines_recoverability_configuration : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_apply_handler_configuration_on_handler_exceptions()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithRecoverabilityHandler>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.FailedMessages.Any())
            .Run();

        Assert.AreEqual(11, context.HandlerInvocations);
        //Assert.AreEqual(typeof(FailingMessage), context.ReceivedMessageType);
    }


    [Test]
    public async Task Should_not_apply_handler_configuration_on_pipeline_exceptions()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithRecoverabilityHandler>(e => e
                .CustomConfig(c => c.Pipeline.Register(new ExceptionChangingBehavior(), "will catch handler exceptions and rethrow a different one"))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.FailedMessages.Any())
            .Run();

        Assert.AreEqual(1, context.HandlerInvocations);
    }

    [Test]
    public async Task Should_apply_handler_configuration_on_pipeline_rethrow()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithRecoverabilityHandler>(e => e
                .CustomConfig(c => c.Pipeline.Register(new ExceptionRethrowingBehavior(), "rethrows exceptions"))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.FailedMessages.Any())
            .Run();

        Assert.AreEqual(11, context.HandlerInvocations);
    }

    class Context : ScenarioContext
    {
        public int HandlerInvocations { get; set; }
        public Type ReceivedMessageType { get; set; }
    }

    class ExceptionChangingBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (Exception e)
            {
                throw new Exception("Custom exception that wraps the original exception", e);
            }
        }
    }

    class ExceptionRethrowingBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            try
            {
                await next();
            }
            catch (Exception e)
            {
                // do some logging or whatever
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw e; // rethrow and change stacktrace
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
        }
    }

    public class EndpointWithRecoverabilityHandler : EndpointConfigurationBuilder
    {
        public EndpointWithRecoverabilityHandler()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.Recoverability()
                    .Immediate(i => i.NumberOfRetries(0))
                    .Delayed(i => i.NumberOfRetries(0));
            });
        }

        class FailingMessageHandler : IHandleMessages<FailingMessage>, IUseRecoverabilityConfiguration<FailingMessageHandler.TestConfiguration>
        {
            Context testContext;

            public FailingMessageHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerInvocations++;

                throw new SimulatedException("handler failure");
            }

            class TestConfiguration : IRecoverabilityConfiguration
            {
                public RecoverabilityConfiguration OnError(Exception exception, object failedMessage, IReadOnlyDictionary<string, string> messageHeaders, ContextBag extensions)
                {
                    //var testContext = (Context)extensions.Get<ScenarioContext>(); // no access
                    //testContext.ReceivedMessageType = failedMessage.GetType();

                    if (exception is SimulatedException)
                    {
                        return new RecoverabilityConfiguration()
                        {
                            MaximumImmediateRetries = 10,
                            MaximumDelayedRetries = 0
                        };
                    }

                    return null;
                }
            }
        }
    }

    class FailingMessage : IMessage
    {
    }
}

