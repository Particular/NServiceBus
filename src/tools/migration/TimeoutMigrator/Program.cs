namespace TimeoutMigrator
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Transactions;
    using NServiceBus;
    using NServiceBus.Config;
    using NServiceBus.Saga;
    using NServiceBus.Unicast.Queuing.Msmq;

    class Program
    {
        static IBus bus;
        static Address newTimeoutManagerAddress;
        static MessageQueue storageQueue;
        static readonly List<Tuple<TimeoutData, string>> timeoutsToBeMigrated = new List<Tuple<TimeoutData, string>>();
        static string[] cmdLine;
        static void Main(string[] args)
        {
            cmdLine = args;

            var timeOfMigration = DateTime.UtcNow;

            var storage = GetSetting("-storageQueue");

            var inputStorageQueue = Address.Parse(storage);

            var destination = GetSetting("-destination");
            newTimeoutManagerAddress = Address.Parse(destination);

            var minAge = GetSetting("-migrateOlderThan");

            var minAgeOfTimeouts = TimeSpan.MinValue;
            if (!string.IsNullOrEmpty(minAge))
                minAgeOfTimeouts = TimeSpan.Parse(minAge);

            bus = Configure.With()
               .DefaultBuilder()
               .XmlSerializer()
               .MsmqTransport()
               .UnicastBus()
               .SendOnly();

            var path = MsmqUtilities.GetFullPath(inputStorageQueue);

            storageQueue = new MessageQueue(path) { MessageReadPropertyFilter = { LookupId = true } };

            if (!storageQueue.Transactional)
                throw new Exception(inputStorageQueue + " must be transactional.");


            storageQueue.Formatter = new XmlMessageFormatter(new[] { typeof(TimeoutData) });

            Console.WriteLine(string.Format("Parsing {0} to find timeouts to migrate", inputStorageQueue));

            storageQueue.GetAllMessages().ToList().ForEach(
                m =>
                {
                    var timeoutData = m.Body as TimeoutData;
                    if (timeoutData == null) //get rid of message
                        throw new InvalidOperationException("Failed to parse timeout data with id " + m.Id);

                    if (minAgeOfTimeouts != TimeSpan.MinValue && timeoutData.Time < (timeOfMigration + minAgeOfTimeouts))
                    {
                        Console.WriteLine(string.Format("Timeout {0} has a expiry ({1}) less than the configured min age of {2} and will be ignored", m.Id, timeoutData.Time, minAgeOfTimeouts));
                        return;
                    }
                    timeoutsToBeMigrated.Add(new Tuple<TimeoutData, string>(timeoutData, m.Id));
                });

            Console.WriteLine(string.Format("{0} parsed, {1} timeouts found that will be migrated", inputStorageQueue, timeoutsToBeMigrated.Count()));

            timeoutsToBeMigrated.ForEach(t => MigrateMessage(t.Item1, t.Item2));

            Console.WriteLine(string.Format("Migration completed successfully"));
        }

        static void MigrateMessage(TimeoutData timeoutData, string messageId)
        {
            //fake the return address of the message
            DestinationOverride.CurrentDestination = Address.Parse(timeoutData.Destination);

            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                bus.Send<TimeoutMessage>(newTimeoutManagerAddress, tm =>
                {
                    tm.ClearTimeout = false; //always false since we don't store the "clear" requests
                    tm.Expires = timeoutData.Time;
                    tm.SagaId = timeoutData.SagaId;
                    tm.State = timeoutData.State;
                });

                storageQueue.ReceiveById(messageId);

                scope.Complete();
            }
        }

        static string GetSetting(string name)
        {

            var index = Array.IndexOf(cmdLine, name);

            if (index < 0)
            {
                Console.WriteLine(name.Replace("-", "") + ":");
                return Console.ReadLine();

            }

            if (cmdLine.Count() > index + 1)
                return cmdLine[index + 1];

            return string.Empty;
        }
    }
}
