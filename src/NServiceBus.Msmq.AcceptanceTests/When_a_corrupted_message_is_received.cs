namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Messaging;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Logging;
    using NUnit.Framework;
    using Transport;

    public class When_a_corrupted_message_is_received : NServiceBusAcceptanceTest
    {
        [TestCase(TransportTransactionMode.TransactionScope)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.None)]
        public async Task Should_move_it_to_the_error_queue(TransportTransactionMode transactionMode)
        {
            DeleteQueue();
            try
            {
                var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b =>
                    {
                        b.CustomConfig((c, ctx) =>
                        {
                            c.UseTransport<MsmqTransport>()
                                .Transactions(transactionMode);
                            c.Recoverability().CustomPolicy((cfg, errorContext) =>
                            {
                                ctx.ErrorContexts.Add(errorContext);
                                return DefaultRecoverabilityPolicy.Invoke(cfg, errorContext);
                            });
                        });
                        b.When((session, c) =>
                        {
                            var endpoint = Conventions.EndpointNamingConvention(typeof(Endpoint));

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
                Assert.AreEqual(1, context.ErrorContexts.Count);
                Assert.True(context.ErrorContexts.All(x => x.Message.IsPoison));
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

        const string errorQueue = @".\private$\errorQueueForCorruptedMessages";

        public class Context : ScenarioContext
        {
            public Context()
            {
                ErrorContexts = new ConcurrentBag<ErrorContext>();
            }

            public ConcurrentBag<ErrorContext> ErrorContexts { get; }
        }


        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => { c.SendFailedMessagesTo("errorQueueForCorruptedMessages"); });
            }
        }
    }
}