namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Messaging;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Logging;
    using NUnit.Framework;

    public class When_a_corrupted_message_is_received : NServiceBusAcceptanceTest
    {
        const string errorQueue = @".\private$\errorQueueForCorruptedMessages";

        [TestCase(TransportTransactionMode.TransactionScope)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.None)]
        public async Task Should_move_it_to_the_error_queue(TransportTransactionMode transactionMode)
        {
            DeleteQueue();
            try
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b =>
                    {
                        b.CustomConfig(c =>
                        {
                            c.UseTransport<MsmqTransport>()
                                .Transactions(transactionMode);
                        });
                        b.When((session, c) =>
                        {
                            var endpoint = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Endpoint));

                            var inputQueue = $@".\private$\{endpoint}";

                            using (var queue = new MessageQueue(inputQueue))
                            using (var message = new Message())
                            {
                                message.Extension = Encoding.UTF8.GetBytes("<badheaders");
                                queue.Send(message, MessageQueueTransactionType.Single);
                            }

                            return Task.FromResult(0);
                        });
                    })
                    .Done(c => c.Logs.Any(l => l.Level == LogLevel.Error))
                    .Run();
                Assert.True(MessageExistsInErrorQueue(), "The message should have been moved to the error queue");
            }
            finally
            {
                DeleteQueue();
            }
        }

        static void DeleteQueue()
        {
            if (MessageQueue.Exists(errorQueue))
            {
                MessageQueue.Delete(errorQueue);
            }
        }

        static bool MessageExistsInErrorQueue()
        {
            if (!MessageQueue.Exists(errorQueue))
            {
                return false;
            }
            using (var queue = new MessageQueue(errorQueue))
            using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
            {
                return message != null;
            }
        }

        public class Context : ScenarioContext
        {
        }


        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendFailedMessagesTo("errorQueueForCorruptedMessages");
                });
            }
        }
    }
}