namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_TimeToBeReceived_set_and_receivetransaction_Msmq : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_send()
        {
            var context = new Context();
            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new MyMessage())))
                    .Repeat(r => r.For<MsmqOnly>())
                    .Run();
            Assert.IsTrue(context.CorrectExceptionThrown);
        }

        public class Context : ScenarioContext
        {
            public bool CorrectExceptionThrown { get; set; }
        }
        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Transactions().Enable().DisableDistributedTransactions());
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    try
                    {
                        Bus.SendLocal(new MyTimeToBeReceivedMessage());
                    }
                    catch (Exception ex)
                    {
                        Context.CorrectExceptionThrown = ex.Message.EndsWith("Sending messages with a custom TimeToBeReceived is not supported on transactional MSMQ.");
                    }
                    
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }

        [Serializable]
        [TimeToBeReceived("00:01:00")]
        public class MyTimeToBeReceivedMessage : IMessage
        {
        }
    }
}
