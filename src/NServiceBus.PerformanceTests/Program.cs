namespace Runner
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus;
    using NServiceBus.Features;
    using Encryption;
    using NServiceBus.Persistence;
    using Saga;
    using System;
    
    class Program
    {
        static Configure config;

        static void Main(string[] args)
        {
            var testCaseToRun = args[0];

            int numberOfThreads;

            if (!int.TryParse(testCaseToRun, out numberOfThreads))
            {
                var testCase = TestCase.Load(testCaseToRun,args);

                testCase.Run();
                testCase.DumpSettings();
              
                return;
            }

            var volatileMode = (args[4].ToLower() == "volatile");
            var suppressDTC = (args[4].ToLower() == "suppressdtc");
            var twoPhaseCommit = (args[4].ToLower() == "twophasecommit");
            var saga = (args[5].ToLower() == "sagamessages");
            var encryption = (args[5].ToLower() == "encryption");
            var concurrency = int.Parse(args[7]);

            TransportConfigOverride.MaximumConcurrencyLevel = numberOfThreads;

            var numberOfMessages = int.Parse(args[1]);

            var endpointName = "PerformanceTest";

            if (volatileMode)
                endpointName += ".Volatile";

            if (suppressDTC)
                endpointName += ".SuppressDTC";

            config = Configure.With(o =>
            {
                o.EndpointName(endpointName);
                o.EnableInstallers();
                o.DiscardFailedMessagesInsteadOfSendingToErrorQueue();
                o.UseTransport<Msmq>().ConnectionString("deadLetter=false;journal=false");
                o.DisableFeature<Audit>();

                if (volatileMode)
                {
                    o.DisableDurableMessages();
                    o.Transactions(false).DoNotWrapHandlersExecutionInATransactionScope().DisableDistributedTransactions();
                    o.UsePersistence<InMemory>();
                }

                switch (args[3].ToLower())
                {
                    case "msmq":
                        o.UseTransport<Msmq>();
                        break;

                    default:
                        throw new InvalidOperationException("Illegal transport " + args[2]);
                }

                if (suppressDTC)
                {
                    o.Transactions().DisableDistributedTransactions();
                }

                switch (args[2].ToLower())
                {
                    case "xml":
                        o.UseSerialization<Xml>();
                        break;

                    case "json":
                        o.UseSerialization<Json>();
                        break;

                    case "bson":
                        o.UseSerialization<Bson>();
                        break;

                    case "bin":
                        o.UseSerialization<Binary>();
                        break;

                    default:
                        throw new InvalidOperationException("Illegal serialization format " + args[2]);
                }
                o.UsePersistence<InMemory>();

                if (encryption)
                {
                    o.RijndaelEncryptionService("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");
                }
            });

            using (var startableBus = config.CreateBus())
            {
                if (saga)
                {
                    SeedSagaMessages(numberOfMessages, endpointName, concurrency);
                }
                else
                {
                    Statistics.SendTimeNoTx = SeedInputQueue(numberOfMessages / 2, endpointName, numberOfThreads, false, twoPhaseCommit, encryption);
                    Statistics.SendTimeWithTx = SeedInputQueue(numberOfMessages / 2, endpointName, numberOfThreads, true, twoPhaseCommit, encryption);
                }

                Statistics.StartTime = DateTime.Now;

                startableBus.Start();

                while (Interlocked.Read(ref Statistics.NumberOfMessages) < numberOfMessages)
                    Thread.Sleep(1000);

                DumpSetting(args);
                Statistics.Dump();
            }
        }

        static void DumpSetting(string[] args)
        {
            Console.Out.WriteLine("---------------- Settings ----------------");
            Console.Out.WriteLine("Threads: {0}, Serialization: {1}, Transport: {2}, Messagemode: {3}",
                                  args[0],
                                  args[2],
                                  args[3],
                                  args[5]);
        }

        static void SeedSagaMessages(int numberOfMessages, string inputQueue, int concurrency)
        {
            var bus = config.Builder.Build<IBus>();

            for (var i = 0; i < numberOfMessages / concurrency; i++)
            {

                for (var j = 0; j < concurrency; j++)
                {
                    bus.Send(inputQueue, new StartSagaMessage
                    {
                        Id = i
                    });
                }
            }

        }

        static TimeSpan SeedInputQueue(int numberOfMessages, string inputQueue, int numberOfThreads, bool createTransaction, bool twoPhaseCommit, bool encryption)
        {
            var sw = new Stopwatch();

            var bus = config.Builder.Build<IBus>();

            sw.Start();
            Parallel.For(
                0,
                numberOfMessages,
                new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads },
                x =>
                {
                    var message = CreateMessage(encryption);
                    message.TwoPhaseCommit = twoPhaseCommit;
                    message.Id = x;

                    if (createTransaction)
                    {
                        using (var tx = new TransactionScope())
                        {
                            bus.Send(inputQueue, message);
                            tx.Complete();
                        }
                    }
                    else
                    {
                        bus.Send(inputQueue, message);
                    }
                });
            sw.Stop();

            return sw.Elapsed;
        }

        public const string EncryptedBase64Value = "encrypted value";
        const string MySecretMessage = "A secret";

        static MessageBase CreateMessage(bool encryption)
        {
            if (encryption)
            {
                // need a new instance of a message each time
                var message = new EncryptionTestMessage
                {
                    Secret = MySecretMessage,
                    CreditCard = new ClassForNesting { EncryptedProperty = MySecretMessage },
                    LargeByteArray = new byte[1], // the length of the array is not the issue now
                    ListOfCreditCards =
                        new List<ClassForNesting>
                        {
                            new ClassForNesting {EncryptedProperty = MySecretMessage},
                            new ClassForNesting {EncryptedProperty = MySecretMessage}
                        }
                };
                message.ListOfSecrets = new ArrayList(message.ListOfCreditCards);

                return message;
            }

            return new TestMessage();
        }
    }
}
