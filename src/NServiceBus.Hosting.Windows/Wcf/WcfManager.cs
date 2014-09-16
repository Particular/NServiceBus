namespace NServiceBus.Hosting.Wcf
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Logging;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    ///     Enable users to expose messages as WCF services
    /// </summary>
    class WcfManager
    {
        internal static IBus Bus;
        /// <summary>
        ///     Starts a <see cref="ServiceHost" /> for each found service. Defaults to <see cref="BasicHttpBinding" /> if
        ///     no user specified binding is found
        /// </summary>
        public void Startup(Unicast.UnicastBus bus)
        {
            Bus = bus;
            var conventions = bus.Builder.Build<Conventions>();
            var components = bus.Builder.Build<IConfigureComponents>();

            foreach (var serviceType in bus.Settings.GetAvailableTypes().Where(t => !t.IsAbstract && IsWcfService(t, conventions)))
            {
                var host = new WcfServiceHost(serviceType);

                Binding binding = new BasicHttpBinding();

                if (components.HasComponent<Binding>())
                {
                    binding = bus.Builder.Build<Binding>();
                }

                host.AddDefaultEndpoint(GetContractType(serviceType),
                    binding
                    , String.Empty);

                hosts.Add(host);

                logger.Debug("Going to host the WCF service: " + serviceType.AssemblyQualifiedName);
                host.Open();
            }
        }

        /// <summary>
        ///     Shuts down the service hosts
        /// </summary>
        public void Shutdown()
        {
            hosts.ForEach(h => h.Close());
        }

        static Type GetContractType(Type t)
        {
            var args = t.BaseType.GetGenericArguments();

            return typeof(IWcfService<,>).MakeGenericType(args[0], args[1]);
        }

        static bool IsWcfService(Type t, Conventions conventions)
        {
            var args = t.GetGenericArguments();
            if (args.Length == 2)
            {
                if (conventions.IsMessageType(args[0]))
                {
                    var wcfType = typeof(WcfService<,>).MakeGenericType(args[0], args[1]);
                    if (wcfType.IsAssignableFrom(t))
                    {
                        return true;
                    }
                }
            }

            if (t.BaseType != null)
            {
                return IsWcfService(t.BaseType, conventions) && !t.IsAbstract;
            }

            return false;
        }


        static ILog logger = LogManager.GetLogger<WcfManager>();
        readonly List<ServiceHost> hosts = new List<ServiceHost>();
    }
}