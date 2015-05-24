﻿namespace NServiceBus.AcceptanceTests.Retries
{
    using System;
    using System.Runtime.Serialization;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class Handler_throws_SerializationException_new : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_retry_the_message_using_flr()
        {
            var context = new Context { Id = Guid.NewGuid() };

            Scenario.Define(context)
                    .WithEndpoint<RetryEndpoint>(b => b.Given((bus, ctx) => bus.SendLocal(new MessageToBeRetried { ContextId = ctx.Id })))
                    .AllowExceptions()
                    .Done(c => c.ForwardedToErrorQueue)
                    .Run();

            Assert.AreEqual(5 + 1, context.NumberOfTimesInvoked);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public int NumberOfTimesInvoked { get; set; }
            public bool ForwardedToErrorQueue { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(b => b.DisableFeature<Features.SecondLevelRetries>());
            }

            class ErrorNotificationSpy : IRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start(IRunContext context)
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ForwardedToErrorQueue = true;
                    });
                }

                public void Stop(IRunContext context) { }
            }


            class MessageToBeRetriedHandler : IProcessCommands<MessageToBeRetried>
            {
                public Context Context { get; set; }

                public void Handle(MessageToBeRetried message, ICommandContext context)
                {
                    if (message.ContextId != Context.Id)
                    {
                        return;
                    }
                    Context.NumberOfTimesInvoked++;
                    throw new SerializationException();
                }
            }
        }

        [Serializable]
        public class MessageToBeRetried : ICommand
        {
            public Guid ContextId { get; set; }
        }
    }
}