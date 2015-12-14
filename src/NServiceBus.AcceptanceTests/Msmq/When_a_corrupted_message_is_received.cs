namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using System.Linq;
    using System.Messaging;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_a_corrupted_message_is_received : NServiceBusAcceptanceTest
    {
        const string errorQueue = @".\private$\errorQueueForCorruptedMessages";

        [TestCase(TransactionSupport.Distributed)]
        [TestCase(TransactionSupport.MultiQueue)]
        [TestCase(TransactionSupport.None)]
        public async Task Should_move_it_to_the_error_queue(TransactionSupport txMode)
        {
            DeleteQueue();
            try
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b =>
                    {
                        b.CustomConfig(c =>
                        {
                            switch (txMode)
                            {
                                case TransactionSupport.Distributed:
                                    c.Transactions().EnableDistributedTransactions();
                                    break;
                                case TransactionSupport.MultiQueue:
                                    c.Transactions().DisableDistributedTransactions();
                                    break;
                                case TransactionSupport.None:
                                    c.Transactions().Disable();
                                    break;
                            }
                        });
                        b.When((bus, c) =>
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
                    .Done(c => c.Logs.Any(l => l.Level == "error"))
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