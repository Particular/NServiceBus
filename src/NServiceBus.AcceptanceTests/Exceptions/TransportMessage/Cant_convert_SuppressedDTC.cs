namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class Cant_convert_SuppressedDTC : Cant_convert
    {
        [Test]
        public void Should_send_message_to_error_queue()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Receiver>(b => b.Given(bus => SendCorruptedMessage("CantConvertSuppressedDTC.Receiver")))
                    .AllowExceptions()
                    .Done(c => c.Logs.Any(l=>l.Level == "error"))
                    .Repeat(r => r.For<MsmqOnly>())
                    .Should(c =>
                    {
                        var logs = c.Logs;
                        Assert.True(logs.Any(l => l.Message.Contains("is corrupt and will be moved to")));
                    })
                    .Run();
        }

        public class Context : ScenarioContext
        {
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(b => b.Transactions().DisableDistributedTransactions())
                    .AddMapping<Message>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(b => b.Transactions().DisableDistributedTransactions());
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
}