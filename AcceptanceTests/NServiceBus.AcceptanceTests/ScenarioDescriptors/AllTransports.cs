namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting.Support;
    using Hosting.Helpers;

    public class AllTransports : ScenarioDescriptor
    {
        public AllTransports()
        {
            AddRange(ActiveTransports);
        }

        static IEnumerable<RunDescriptor> ActiveTransports
        {
            get
            {
                if (activeTransports == null)
                {
                    activeTransports = new List<RunDescriptor>();

                    var specificTransport = Environment.GetEnvironmentVariable("Transport.UseSpecific");

                    var excludedTransports = Environment.GetEnvironmentVariable("Transport.Excluded");

                    foreach (var transport in Transports.AllAvailable)
                    {
                        var key = transport.Key;

                        if (!string.IsNullOrEmpty(specificTransport) && specificTransport != key)
                        {
                            Console.Out.WriteLine("Transport {0} excluded since the test suite is only specified to run for {1}", key, specificTransport);
                            continue;
                        }
                        if (!string.IsNullOrEmpty(excludedTransports) && excludedTransports.Contains(key))
                        {
                            Console.Out.WriteLine("Transport {0} excluded since its included in the list of exclude transports {1}", key, excludedTransports);
                            continue;
                        }

                        activeTransports.Add(transport);
                    } 
                }

                return activeTransports;
            }
        }

        static ICollection<RunDescriptor> activeTransports;
    }

    public class AllDtcTransports : AllTransports
    {
        public AllDtcTransports()
        {   
            Remove(Transports.RabbitMQ);
        }
    }

    public class AllBrokerTransports : AllTransports
    {
        public AllBrokerTransports()
        {
            Remove(Transports.Msmq);
        }
    }

    public class AllTransportsWithCentralizedPubSubSupport : AllTransports
    {
        public AllTransportsWithCentralizedPubSubSupport()
        {
            Remove(Transports.Msmq);
            Remove(Transports.SqlServer);
        }
    }

    public class AllTransportsWithMessageDrivenPubSub : AllTransports
    {
        public AllTransportsWithMessageDrivenPubSub()
        {
            Remove(Transports.ActiveMQ);
            Remove(Transports.RabbitMQ);
        }
    }

    public class TypeScanner
    {

        public static IEnumerable<Type> GetAllTypesAssignableTo<T>()
        {
            return AvailableAssemblies.SelectMany(a => a.GetTypes())
                                      .Where(t => typeof (T).IsAssignableFrom(t) && t != typeof(T))
                                      .ToList();
        }

        static IEnumerable<Assembly> AvailableAssemblies
        {
            get
            {
                if (assemblies == null)
                {
                    var result = AssemblyScanner.GetScannableAssemblies();



                    if (result.Errors.Any())
                    {
                        foreach (var errors in result.Errors)
                        {
                            Console.Out.WriteLine(errors);
                        }

                        throw new InvalidOperationException("Assembly scanning failed");
                    }

                    

                    assemblies = result.Assemblies;
                }
                    
                return assemblies;
            }
        }

        static List<Assembly> assemblies;
    }
}