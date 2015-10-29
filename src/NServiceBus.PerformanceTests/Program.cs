﻿namespace Runner
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
    using Saga;
    using System;
    using MsmqTransport = NServiceBus.MsmqTransport;

    class Program
    {
        static void Main(string[] args)
        {
            var testCaseToRun = args[0];

            int numberOfThreads;

            if (!int.TryParse(testCaseToRun, out numberOfThreads))
            {
                var testCase = TestCase.Load(testCaseToRun, args);

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

            var configuration = new BusConfiguration();

            configuration.EndpointName(endpointName);
            configuration.EnableInstallers();
            configuration.UseTransport<MsmqTransport>().ConnectionString("deadLetter=false;journal=false");
            configuration.DisableFeature<Audit>();

            if (volatileMode)
            {
                configuration.DisableDurableMessages();
                configuration.UsePersistence<InMemoryPersistence>();
            }

            switch (args[3].ToLower())
            {
                case "msmq":
                    configuration.UseTransport<MsmqTransport>();
                    break;

                default:
                    throw new InvalidOperationException("Illegal transport " + args[2]);
            }

            if (suppressDTC)
            {
                configuration.Transactions().DisableDistributedTransactions();
            }

            switch (args[2].ToLower())
            {
                case "xml":
                    configuration.UseSerialization<XmlSerializer>();
                    break;

                case "json":
                    configuration.UseSerialization<JsonSerializer>();
                    break;

                default:
                    throw new InvalidOperationException("Illegal serialization format " + args[2]);
            }
            configuration.UsePersistence<InMemoryPersistence>();
            configuration.RijndaelEncryptionService("gdDbqRpqdRbTs3mhdZh9qCaDaxJXl+e6");

            if (saga)
            {
                configuration.RunWhenEndpointStartsAndStops(new StartActionRunner(b =>
                {
                    SeedSagaMessages(b, numberOfMessages, endpointName, concurrency);
                }));
            }
            else
            {
                configuration.RunWhenEndpointStartsAndStops(new StartActionRunner(b =>
                {
                    Statistics.SendTimeNoTx = SeedInputQueue(b, numberOfMessages / 2, endpointName, numberOfThreads, false, twoPhaseCommit, encryption);
                    Statistics.SendTimeWithTx = SeedInputQueue(b, numberOfMessages / 2, endpointName, numberOfThreads, true, twoPhaseCommit, encryption);
                }));
            }

            var startableBus = Endpoint.Create(configuration).Initialize().GetAwaiter().GetResult();

            Statistics.StartTime = DateTime.Now;
            var stoppable = startableBus.StartAsync().GetAwaiter().GetResult();
            while (Interlocked.Read(ref Statistics.NumberOfMessages) < numberOfMessages)
            {
                Thread.Sleep(1000);
            }
            stoppable.StopAsync().GetAwaiter().GetResult();

            DumpSetting(args);
            Statistics.Dump();
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
        
        static void SeedSagaMessages(ISendOnlyBus bus, int numberOfMessages, string inputQueue, int concurrency)
        {
            for (var i = 0; i < numberOfMessages / concurrency; i++)
            {

                for (var j = 0; j < concurrency; j++)
                {
                    bus.SendAsync(inputQueue, new StartSagaMessage
                    {
                        Id = i
                    }).GetAwaiter().GetResult();
                }
            }
        }

        static TimeSpan SeedInputQueue(ISendOnlyBus bus, int numberOfMessages, string inputQueue, int numberOfThreads, bool createTransaction, bool twoPhaseCommit, bool encryption)
        {
            var sw = new Stopwatch();


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
                        using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                        {
                            bus.SendAsync(inputQueue, message).GetAwaiter().GetResult();
                            tx.Complete();
                        }
                    }
                    else
                    {
                        bus.SendAsync(inputQueue, message).GetAwaiter().GetResult();
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
