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
            AddRange(Transports.AllAvailable);
        }
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
                    assemblies = AssemblyScanner.GetScannableAssemblies().Assemblies;

                return assemblies;
            }
        }

        static List<Assembly> assemblies;
    }
}