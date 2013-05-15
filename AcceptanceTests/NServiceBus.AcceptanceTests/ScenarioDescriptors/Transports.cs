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
                    availableTransports = GetAllAvailable().ToList();

                return availableTransports;
            }
        }


        public static RunDescriptor Default
        {
            get
            {
                var transportsOtherThanMsmq = AllAvailable.Where(t => t != Msmq);

                if (transportsOtherThanMsmq.Count() == 1)
                    return transportsOtherThanMsmq.First();

                return Msmq;
            }
        }

        public static RunDescriptor ActiveMQ
        {
            get { return AllAvailable.Single(r => r.Key == "ActiveMQ"); }
        }

        public static RunDescriptor Msmq
        {
            get { return AllAvailable.Single(r => r.Key == "Msmq"); }
        }

        public static RunDescriptor RabbitMQ
        {
            get { return AllAvailable.Single(r => r.Key == "RabbitMQ"); }
        }



        public static RunDescriptor SqlServer
        {
            get { return AllAvailable.Single(r => r.Key == "SqlServer"); }
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
                {typeof (RabbitMQ).Name, "host=localhost"},
                {typeof (SqlServer).Name, @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"},
                {typeof (ActiveMQ).Name, @"ServerUrl=activemq:tcp://localhost:61616"},
                {typeof (Msmq).Name, @"cacheSendConnection=false;journal=false;"}
            };


    }
}