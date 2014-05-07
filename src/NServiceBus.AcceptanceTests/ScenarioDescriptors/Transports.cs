namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using NServiceBus.Transports;

    public static class Transports
    {
        public static IEnumerable<RunDescriptor> AllAvailable
        {
            get
            {
                if (availableTransports == null)
                {
                    availableTransports = GetAllAvailable().ToList();
                }

                return availableTransports;
            }
        }


        public static RunDescriptor Default
        {
            get
            {
                var specificTransport = Environment.GetEnvironmentVariable("Transport.UseSpecific");

                if (!string.IsNullOrEmpty(specificTransport))
                    return AllAvailable.Single(r => r.Key == specificTransport);

                var transportsOtherThanMsmq = AllAvailable.Where(t => t != Msmq);

                if (transportsOtherThanMsmq.Count() == 1)
                    return transportsOtherThanMsmq.First();

                return Msmq;
            }
        }

        static RunDescriptor Msmq
        {
            get { return AllAvailable.SingleOrDefault(r => r.Key == "Msmq"); }
        }

        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            var foundTransportDefinitions = TypeScanner.GetAllTypesAssignableTo<TransportDefinition>();

            
            foreach (var transportDefinitionType in foundTransportDefinitions)
            {
                var key = transportDefinitionType.Name;

                var runDescriptor = new RunDescriptor
                {
                    Key = key,
                    Settings =
                        new Dictionary<string, string>
                                {
                                    {"Transport", transportDefinitionType.AssemblyQualifiedName}
                                }
                };

                var connectionString = Environment.GetEnvironmentVariable(key + ".ConnectionString");

                if (string.IsNullOrEmpty(connectionString) && DefaultConnectionStrings.ContainsKey(key))
                    connectionString = DefaultConnectionStrings[key];


                if (!string.IsNullOrEmpty(connectionString))
                {
                    runDescriptor.Settings.Add("Transport.ConnectionString", connectionString);
                    yield return runDescriptor;
                }
                else
                {
                    Console.Out.WriteLine("No connection string found for transport: {0}, test will not be executed for this transport", key);
                }
            }
        }

        static IList<RunDescriptor> availableTransports;

        static readonly Dictionary<string, string> DefaultConnectionStrings = new Dictionary<string, string>
            {
                {"RabbitMQ", "host=localhost"},
                {"SqlServer", @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"},
                {"ActiveMQ", @"ServerUrl=activemq:tcp://localhost:61616"},
                {"Msmq", @"cacheSendConnection=false;journal=false;"}
            };


    }
}