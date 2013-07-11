namespace NServiceBus.AcceptanceTests.BusStartStop
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using AcceptanceTesting.Support;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_bus_start_and_stops_with_a_pending_message : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_not_throw()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => SendMessages(b))
                    .Run(TimeSpan.FromMilliseconds(10));

            Thread.Sleep(100);
        }


        EndpointBehaviorBuilder<Context> SendMessages(EndpointBehaviorBuilder<Context> b)
        {
            return b.Given((bus, context) => bus.SendLocal(new Message()));
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MessageHandler : IHandleMessages<Message>
            {
                public void Handle(Message message)
                {
                    Debug.WriteLine("sdfdsf");
                    Thread.Sleep(100);
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }

    }


}