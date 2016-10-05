namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Transport;

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

                    var connectionString = EnvironmentHelper.GetEnvironmentVariable(key + ".ConnectionString");

                    if (string.IsNullOrEmpty(connectionString) && DefaultConnectionStrings.ContainsKey(key))
                    {
                        connectionString = DefaultConnectionStrings[key];
                    }

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        runDescriptor.Settings.Set("Transport.ConnectionString", connectionString);
                        yield return runDescriptor;
                    }
                    else
                    {
                        var transportDefinition = (TransportDefinition) Activator.CreateInstance(transportDefinitionType);

                        if (!transportDefinition.RequiresConnectionString)
                        {
                            yield return runDescriptor;
                        }
                    }
                }
            }
        }

        public static RunDescriptor Default
        {
            get
            {
                var specificTransport = EnvironmentHelper.GetEnvironmentVariable("Transport.UseSpecific");

                var runDescriptors = AllAvailable.ToList();
                if (!string.IsNullOrEmpty(specificTransport))
                {
                    return runDescriptors.Single(r => r.Key == specificTransport);
                }

                var transportsOtherThanMsmq = runDescriptors.Where(t => t.Key != MsmqDescriptorKey).ToList();

                if (transportsOtherThanMsmq.Count == 1)
                {
                    return transportsOtherThanMsmq.First();
                }

                return runDescriptors.Single(t => t.Key == MsmqDescriptorKey);
            }
        }

        static string MsmqDescriptorKey = "MsmqTransport";
        static Lazy<List<Type>> foundDefinitions = new Lazy<List<Type>>(() => TypeScanner.GetAllTypesAssignableTo<TransportDefinition>().ToList());

        static Dictionary<string, string> DefaultConnectionStrings = new Dictionary<string, string>
        {
            {"RabbitMQTransport", "host=localhost"},
            {"SqlServerTransport", @"Server=localhost\sqlexpress;Database=nservicebus;Trusted_Connection=True;"},
            {"MsmqTransport", "cacheSendConnection=false;journal=false;"}
        };
    }
}