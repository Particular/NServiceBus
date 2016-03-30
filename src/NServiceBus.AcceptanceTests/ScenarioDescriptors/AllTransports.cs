namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AcceptanceTesting.Support;
    using NServiceBus.Hosting.Helpers;

    public class AllTransports : ScenarioDescriptor
    {
        protected AllTransports()
        {
            AddRange(ActiveTransports);
        }

        static IEnumerable<RunDescriptor> ActiveTransports => new List<RunDescriptor>
        {
            Transports.Default
        };
    }

    public class AllDtcTransports : AllTransports
    {
        public AllDtcTransports()
        {
            ScenarioFilter.Run(this, Remove);
        }
    }

    public class AllNativeMultiQueueTransactionTransports : AllTransports
    {
        public AllNativeMultiQueueTransactionTransports()
        {
            ScenarioFilter.Run(this, Remove);
        }
    }

    public class AllTransportsWithCentralizedPubSubSupport : AllTransports
    {
        public AllTransportsWithCentralizedPubSubSupport()
        {
            ScenarioFilter.Run(this, Remove);
        }
    }

    public class AllTransportsWithMessageDrivenPubSub : AllTransports
    {
        public AllTransportsWithMessageDrivenPubSub()
        {
            ScenarioFilter.Run(this, Remove);
        }
    }

    public class AllTransportsWithoutNativeDeferral : AllTransports
    {
        public AllTransportsWithoutNativeDeferral()
        {
            ScenarioFilter.Run(this, Remove);
        }
    }

    public class AllTransportsWithoutNativeDeferralAndWithAtomicSendAndReceive : AllTransports
    {
        public AllTransportsWithoutNativeDeferralAndWithAtomicSendAndReceive()
        {
            ScenarioFilter.Run(this, Remove);
        }
    }

    public class TypeScanner
    {
        static IEnumerable<Assembly> AvailableAssemblies
        {
            get
            {
                if (assemblies == null)
                {
                    var result = new AssemblyScanner().GetScannableAssemblies();

                    assemblies = result.Assemblies.Where(a =>
                    {
                        var references = a.GetReferencedAssemblies();

                        return references.All(an => an.Name != "nunit.framework");
                    }).ToList();
                }

                return assemblies;
            }
        }

        public static IEnumerable<Type> GetAllTypesAssignableTo<T>()
        {
            return AvailableAssemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(T).IsAssignableFrom(t) && t != typeof(T))
                .ToList();
        }

        static List<Assembly> assemblies;
    }

    public static class ScenarioFilter
    {
        public static void Run(ScenarioDescriptor scenarioDescriptor, Func<RunDescriptor, bool> remove)
        {
            var runDescriptors = Transports.AllAvailable;
            foreach (var rundescriptor in runDescriptors)
            {
                Type type;
                if (rundescriptor.Settings.TryGet("Transport", out type))
                {
                    var configurerTypeName = "ConfigureScenariosFor" + type.Name;
                    var configurerType = Type.GetType(configurerTypeName, false);

                    if (configurerType == null)
                    {
                        throw new InvalidOperationException($"Acceptance Test project must include a non-namespaced class named '{configurerTypeName}' implementing {typeof(IConfigureSupportedScenariosForTestExecution).Name}. See {typeof(ConfigureScenariosForMsmqTransport).FullName} for an example.");
                    }

                    var configurer = Activator.CreateInstance(configurerType) as IConfigureSupportedScenariosForTestExecution;

                    if (configurer == null)
                    {
                        throw new InvalidOperationException($"{configurerTypeName} does not implement {typeof(IConfigureSupportedScenariosForTestExecution).Name}.");
                    }

                    if (configurer.UnsupportedScenarioDescriptorTypes.Contains(scenarioDescriptor.GetType()))
                    {
                        remove(rundescriptor);
                    }
                }
            }
        }
    }
}