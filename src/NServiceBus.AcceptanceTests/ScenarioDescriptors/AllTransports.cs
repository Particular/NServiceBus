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
        public static IEnumerable<Type> GetAllTypesAssignableTo<T>()
        {
            return AvailableAssemblies.SelectMany(a => a.GetTypes())
                                      .Where(t => typeof(T).IsAssignableFrom(t) && t != typeof(T))
                                      .ToList();
        }

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

        static List<Assembly> assemblies;
    }

    public static class ScenarioFilter
    {
        public static void Run(ScenarioDescriptor scenarioDescriptor, Func<RunDescriptor, bool> remove)
        {

            foreach (var rundescriptor in Transports.AllAvailable)
            {
                var transportAssemblyQualifiedName = rundescriptor.Settings["Transport"];
                var type = Type.GetType(transportAssemblyQualifiedName);
                if (type != null)
                {
                    var configurerTypeName = "Configure" + type.Name;
                    var configurerType = Type.GetType(configurerTypeName, false);

                    if (configurerType == null)
                        throw new InvalidOperationException($"Acceptance Test project must include a non-namespaced class named '{configurerTypeName}' implementing {typeof(IConfigureTestExecution).Name}. See {typeof(ConfigureMsmqTransport).FullName} for an example.");

                    var configurer = Activator.CreateInstance(configurerType) as IConfigureTestExecution;

                    if (configurer == null)
                        throw new InvalidOperationException($"{configurerTypeName} does not implement {typeof(IConfigureTestExecution).Name}.");


                    if (configurer.UnsupportedScenarioDescriptorTypes.Contains(scenarioDescriptor.GetType()))
                    {
                        remove(rundescriptor);
                    }
                }
            }
        }
    }
}