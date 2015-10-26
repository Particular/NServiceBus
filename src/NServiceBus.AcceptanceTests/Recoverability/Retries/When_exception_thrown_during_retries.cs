namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    public class When_exception_thrown_during_retries : NServiceBusAcceptanceTest
    {
        protected class RegisterBlowupBehavior : RegisterStep
        {
            public RegisterBlowupBehavior(string previousStepName) : base("BlowUpBehavior", typeof(BlowUpAfterDispatchBehavior), "Always throws exception")
            {
                InsertAfter(previousStepName);
            }

        }
        class BlowUpAfterDispatchBehavior : Behavior<TransportReceiveContext>
        {
            public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
            {
                await next().ConfigureAwait(false);

                throw new SimulatedException();
            }
        }
        public class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.PurgeOnStartup(true);
                })
                .WithConfig<TransportConfig>(c => c.MaximumConcurrencyLevel = 1);
            }

            class ErrorQueueHandler : IHandleMessages<SideEffectMessage>, IHandleMessages<FailingMessage>
            {
                public Context Context { get; set; }

                public Task Handle(SideEffectMessage message, IMessageHandlerContext context)
                {
                    Context.SideEffectMessageReceived = true;

                    return Task.FromResult(0);
                }

                public Task Handle(FailingMessage message, IMessageHandlerContext context)
                {
                    Context.FailingMessageMovedToErrorQueueAndProcessedByErrorSpy = true;

                    return Task.FromResult(0);
                }
            }
        }
        protected class Context : ScenarioContext
        {
            public bool FailingMessageMovedToErrorQueueAndProcessedByErrorSpy { get; set; }
            public bool SideEffectMessageReceived { get; set; }
        }

        protected class FailingMessage : IMessage
        {
        }

        protected class SideEffectMessage : IMessage
        {
        }
    }
}
