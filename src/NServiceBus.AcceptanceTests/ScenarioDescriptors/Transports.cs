namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Transports;

    public static class Transports
    {
        internal static IEnumerable<RunDescriptor> AllAvailable
        {
            get
            {
                foreach (var transportDefinitionType in foundDefinitions.Value)
                {
                    var key = transportDefinitionType.Name;

                    var runDescriptor = new RunDescriptor(key);
                    runDescriptor.Settings.Set("Transport", transportDefinitionType);

                    var connectionString = Environment.GetEnvironmentVariable(key + ".ConnectionString");

                    if (string.IsNullOrEmpty(connectionString) && DefaultConnectionStrings.ContainsKey(key))
                    {
                        connectionString = DefaultConnectionStrings[key];
                    }


                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        runDescriptor.Settings.Set("Transport.ConnectionString", connectionString);
                        yield return runDescriptor;
                    }
                }
            }
        }

        public static RunDescriptor Default
        {
            get
            {
                var specificTransport = EnvironmentHelper.GetEnvironmentVariable("Transport.UseSpecific");

                var runDescriptors = AllAvailable;
                if (!string.IsNullOrEmpty(specificTransport))
                {
                    return runDescriptors.Single(r => r.Key == specificTransport);
                }

                var transportsOtherThanMsmq = runDescriptors.Where(t => t != Msmq);

                if (transportsOtherThanMsmq.Count() == 1)
                {
                    return transportsOtherThanMsmq.First();
                }

                return Msmq;
            }
        }

        public static RunDescriptor Msmq
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "MsmqTransport"); }
        }

        static Lazy<List<Type>> foundDefinitions = new Lazy<List<Type>>(() => TypeScanner.GetAllTypesAssignableTo<TransportDefinition>().ToList());

        static Dictionary<string, string> DefaultConnectionStrings = new Dictionary<string, string>
        {
            {"RabbitMQTransport", "host=localhost"},
            {"SqlServerTransport", @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"},
            {"MsmqTransport", @"cacheSendConnection=false;journal=false;"}
        };
    }
}