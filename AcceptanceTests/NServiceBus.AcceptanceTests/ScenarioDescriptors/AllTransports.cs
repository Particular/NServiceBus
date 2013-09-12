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
                    //temporary fix until we can get rid of the "AllTransports" all together
                    activeTransports = new List<RunDescriptor>
                    {
                        Transports.Default
                    }; 
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