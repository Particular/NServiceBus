namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting.Support;
    using Hosting.Helpers;
    using NServiceBus.Transports;

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
            AllTransportsFilter.Run(t => t.HasSupportForDistributedTransactions.HasValue
                                         && !t.HasSupportForDistributedTransactions.Value, Remove);
        }
    }

    public class AllBrokerTransports : AllTransports
    {
        public AllBrokerTransports()
        {
            AllTransportsFilter.Run(t => !t.HasNativePubSubSupport, Remove);
        }
    }

    public class AllTransportsWithCentralizedPubSubSupport : AllTransports
    {
        public AllTransportsWithCentralizedPubSubSupport()
        {
            AllTransportsFilter.Run(t => !t.HasSupportForCentralizedPubSub, Remove);
        }
    }

    public class AllTransportsWithMessageDrivenPubSub : AllTransports
    {
        public AllTransportsWithMessageDrivenPubSub()
        {
            AllTransportsFilter.Run(t => t.HasNativePubSubSupport, Remove);
        }
    }

    public class MsmqOnly : ScenarioDescriptor
    {
        public MsmqOnly()
        {
            if (Transports.Default == Transports.Msmq)
            {
                Add(Transports.Msmq);
            }
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
                    var result = new AssemblyScanner().GetScannableAssemblies();

                    assemblies = result.Assemblies;
                }
                    
                return assemblies;
            }
        }

        static List<Assembly> assemblies;
    }

    public static class AllTransportsFilter
    {
        public static void Run(Func<TransportDefinition, bool> condition, Func<RunDescriptor, bool> remove)
        {
            foreach (var rundescriptor in Transports.AllAvailable)
            {
                var transportAssemblyQualifiedName = rundescriptor.Settings["Transport"];
                var type = Type.GetType(transportAssemblyQualifiedName);
                if (type != null)
                {
                    var transport = Activator.CreateInstance(type, true) as TransportDefinition;
                    if (condition(transport))
                    {
                        remove(rundescriptor);
                    }
                }
            }
        }
    }
}