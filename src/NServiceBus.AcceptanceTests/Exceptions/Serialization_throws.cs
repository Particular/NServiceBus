﻿namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class Serialization_throws : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_receive_MessageDeserializationException()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.When(c => c.Subscribed, bus => bus.SendLocal(new Message())))
                    .AllowExceptions()
                    .Done(c => c.ExceptionReceived)
                    .Run();

            Assert.AreEqual(typeof(MessageDeserializationException), context.ExceptionType);
        }

        public class Context : ScenarioContext
        {
            public bool ExceptionReceived { get; set; }
            public Type ExceptionType { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.Pipeline.Register("Corruption", typeof(CorruptionBehavior), "The corruption behavior");
                    b.RegisterComponents(c => c.ConfigureComponent<CorruptionBehavior>(DependencyLifecycle.InstancePerCall));
                    b.DisableFeature<TimeoutManager>();
                    b.DisableFeature<SecondLevelRetries>();
                })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0;
                    });
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public void Start()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ExceptionType = e.Exception.GetType();
                        Context.ExceptionReceived = true;
                    });

                    Context.Subscribed = true;
                }

                public void Stop() { }
            }


            class CorruptionBehavior : PhysicalMessageProcessingStageBehavior
            {
                public override Task Invoke(Context context, Func<Task> next)
                {
                    context.GetPhysicalMessage().Body[1]++;
                    return next();
                }
            }

            class Handler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                }
            }
        }

        public class Message : IMessage
        {
        }
    }

}