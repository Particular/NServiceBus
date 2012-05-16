using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Wcf
{
    /// <summary>
    /// Enable users to expose messages as WCF services
    /// </summary>
    public class WcfManager
    {
        private readonly List<Type> serviceTypes = new List<Type>();
        private readonly List<ServiceHost> hosts = new List<ServiceHost>();
      
        /// <summary>
        /// Initlalized the manager with the list of assemblies to be scanned
        /// </summary>
        /// <param name="assembliesToScan"></param>
        public WcfManager(IEnumerable<Assembly> assembliesToScan)
        {
            foreach (var a in assembliesToScan)
                foreach (var t in a.GetTypes())
                    if (IsWcfService(t) && !t.IsAbstract)
                        serviceTypes.Add(t);
        }
       
        /// <summary>
        /// Starts a servicehost for each found service. Defaults to BasicHttpBinding if
        /// no user specified binding is found
        /// </summary>
        public void Startup()
        {
            foreach (var serviceType in serviceTypes)
            {
                var host = new WcfServiceHost(serviceType);

                Binding binding = new BasicHttpBinding();

                if (Configure.Instance.Configurer.HasComponent<Binding>())
                    binding = Configure.Instance.Builder.Build<Binding>();
                
                host.AddDefaultEndpoint(   GetContractType(serviceType),
                                           binding
                                           ,"");

                hosts.Add(host);

                logger.Debug("Going to host the WCF service: " + serviceType.AssemblyQualifiedName);
                host.Open();
            }
        }

        /// <summary>
        /// Shutsdown the service hosts
        /// </summary>
        public void Shutdown()
        {
            hosts.ForEach(h => h.Close());
        }

        private static Type GetContractType(Type t)
        {
            var args = t.BaseType.GetGenericArguments();

            return typeof(IWcfService<,>).MakeGenericType(args);
        }

        private static bool IsWcfService(Type t)
        {
            var args = t.GetGenericArguments();
            if (args.Length == 2)
                if (typeof(IMessage).IsAssignableFrom(args[0]))
                {
                    var wcfType = typeof(WcfService<,>).MakeGenericType(args);
                    if (wcfType.IsAssignableFrom(t))
                        return true;
                }

            if (t.BaseType != null)
                return IsWcfService(t.BaseType) && !t.IsAbstract;

            return false;
        }

       
        private readonly ILog logger = LogManager.GetLogger(typeof(WcfManager));
    }
}