namespace NServiceBus.AcceptanceTests.BasicMessaging
{
    using System;
    using System.Threading;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_using_a_message_with_TimeToBeReceived_has_expired : NServiceBusAcceptanceTest
    {
        [Test]
        public void Message_should_not_be_received()
        {
            var context = new Context();

            var timeToWaitFor = DateTime.Now.AddSeconds(10);
            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new MyMessage())))
                    .Done(c => DateTime.Now > timeToWaitFor )
                    .Run();
            Assert.IsFalse(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                static bool hasSkipped;
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    if (!hasSkipped)
                    {
                        hasSkipped = true;
                        Thread.Sleep(TimeSpan.FromSeconds(2));
                        Bus.HandleCurrentMessageLater();
                        return;
                    }
                    Context.WasCalled = true;
                }
            }
        }

        [Serializable]
        [TimeToBeReceived("00:00:01")]
        public class MyMessage : IMessage
        {
        }
    }
}
