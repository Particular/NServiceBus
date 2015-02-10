namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;
    using IMessage = NServiceBus.IMessage;

    public class When_cant_convert_to_TransportMessage_NoTransactions : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_send_message_to_error_queue()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given(bus => bus.Send(new Message())))
                    .WithEndpoint<Receiver>()
                    .AllowExceptions()
                    .Done(c => c.GetAllLogs().Any(l=>l.Level == "error"))
                    .Repeat(r => r.For<MsmqOnly>())
                    .Should(c =>
                    {
                        var logs = c.GetAllLogs();
                        Assert.True(logs.Any(l => l.Message.Contains("is corrupt and will be moved to")));
                    })
                    .Run(new RunSettings
                         {
                             UseSeparateAppDomains = true
                         });
        }

        public class Context : ScenarioContext
        {
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(b => b.Transactions().Disable())
                    .AddMapping<Message>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                SerializerCorrupter.Corrupt();
                EndpointSetup<DefaultServer>(b => b.Transactions().Disable());
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
}