namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_using_per_uow_component_in_the_pipeline : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task It_should_be_scoped_to_uow_both_in_behavior_and_in_the_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(e => e
                    .When(async s =>
                    {
                        await SendMessage(s).ConfigureAwait(false);
                        await SendMessage(s).ConfigureAwait(false);
                    }))
                .Done(c => c.MessagesProcessed >= 2)
                .Run();

            Assert.IsFalse(context.ValueEmpty, "Empty value in the UoW component meaning the UoW component has been registered as per-call");
            Assert.IsFalse(context.ValueAlreadyInitialized, "Value in the UoW has already been initialized when it was resolved for the first time in a given pipeline meaning the UoW component has been registered as a singleton.");
        }

        static Task SendMessage(IMessageSession s)
        {
            var uniqueValue = Guid.NewGuid().ToString();
            var options = new SendOptions();
            options.RouteToThisEndpoint();
            options.SetHeader("Value", uniqueValue);
            var message = new Message
            {
                Value = uniqueValue
            };

            return s.Send(message, options);
        }

        class Context : ScenarioContext
        {
            int messagesProcessed;
            public int MessagesProcessed => messagesProcessed;

            public void OnMessageProcessed()
            {
                Interlocked.Increment(ref messagesProcessed);
            }

            public bool ValueEmpty { get; set; }
            public bool ValueAlreadyInitialized { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.RegisterComponents(r => r.AddScoped<UnitOfWorkComponent>());
                    c.Pipeline.Register(b => new HeaderProcessingBehavior(b.GetService<Context>()), "Populates UoW component.");
                    c.LimitMessageProcessingConcurrencyTo(1);
                });
            }

            class HeaderProcessingBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
            {
                Context testContext;

                public HeaderProcessingBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
                {
                    var uowScopeComponent = context.Builder.GetService<UnitOfWorkComponent>();
                    testContext.ValueAlreadyInitialized |= uowScopeComponent.ValueFromHeader != null;
                    uowScopeComponent.ValueFromHeader = context.MessageHeaders["Value"];

                    return next(context);
                }
            }

            class UnitOfWorkComponent
            {
                public string ValueFromHeader { get; set; }
            }

            class Handler : IHandleMessages<Message>
            {
                public Handler(Context testContext, UnitOfWorkComponent component)
                {
                    this.testContext = testContext;
                    this.component = component;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.ValueEmpty |= component.ValueFromHeader == null;
                    testContext.OnMessageProcessed();
                    return Task.FromResult(0);
                }

                Context testContext;
                UnitOfWorkComponent component;
            }
        }

        public class Message : IMessage
        {
            public string Value { get; set; }
        }
    }
}