namespace NServiceBus.AcceptanceTests.Exceptions
{
    using System;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class Cant_convert_NoTransactions : Cant_convert
    {
        [Test]
        public void Should_send_message_to_error_queue()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Receiver>(b => b.Given(bus => SendCorruptedMessage("CantConvertNoTransactions.Receiver")))
                    .AllowExceptions()
                    .Done(c =>
                    {
                        var logs = c.Logs;
                        return logs.Any(l => l.Level == "error");
                    })
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

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(b => b.Transactions().Disable());
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }
}