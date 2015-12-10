﻿namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    public class When_fails_with_retries_set_to_0 : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_retry_the_message_using_flr()
        {
            RetryEndpoint.SentToErrorQueue = false;
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<RetryEndpoint>(b => b
                        .DoNotFailOnErrorMessages())
                    .Done(c => RetryEndpoint.SentToErrorQueue)
                    .Run();

            Assert.AreEqual(1, context.NumberOfTimesInvoked, "No FLR should be in use if MaxRetries is set to 0");
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int NumberOfTimesInvoked { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.DisableFeature<Features.SecondLevelRetries>();
                    b.Faults().AddFaultNotification(message => SentToErrorQueue = true);
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            public static bool SentToErrorQueue;

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }
                public Task Start(IBusContext context)
                {
                    return context.SendLocal(new MessageToBeRetried
                    {
                        ContextId = Context.Id
                    });
                }

                public Task Stop(IBusContext context)
                {
                    return Task.FromResult(0);
                }
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context Context { get; set; }
                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    if (Context.Id != message.ContextId)
                    {
                        return Task.FromResult(0);
                    }
                    Context.NumberOfTimesInvoked++;
                    throw new SimulatedException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : IMessage
        {
            public Guid ContextId { get; set; }
        }
    }


}