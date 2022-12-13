namespace NServiceBus.AcceptanceTests.Recoverability;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NServiceBus.Recoverability;
using NUnit.Framework;

public class When_handler_defines_recoverability_configuration : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_apply_configuration_on_handler_exceptions()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithRecoverabilityHandler>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.FailedMessages.Any())
            .Run();

        Assert.AreEqual(11, context.HandlerInvocations);
    }


    [Test]
    public async Task Should_apply_configuration_on_pipeline_exceptions()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithRecoverabilityHandler>(e => e
                .CustomConfig(c => c.Pipeline.Register(new ExceptionChangingBehavior(), "will catch handler exceptions and rethrow a different one"))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.FailedMessages.Any())
            .Run();

        Assert.AreEqual(11, context.HandlerInvocations);
    }

    class Context : ScenarioContext
    {
        public int HandlerInvocations { get; set; }
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

        class FailingMessageHandler : IHandleMessages<FailingMessage>
        {
            Context testContext;

            public FailingMessageHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerInvocations++;

                context.UseRecoverabilityConfiguration(new ImmediateConfig(10), new DelayedConfig(0, TimeSpan.Zero));

                throw new SimulatedException("handler failure");
            }
        }
    }

    class FailingMessage : IMessage
    {
    }
}

