using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Common.Logging;

namespace NServiceBus.Hosting.Wcf
{
    public class WcfManager
    {
        private readonly List<Type> serviceTypes = new List<Type>();
        private readonly List<ServiceHost> hosts = new List<ServiceHost>();
      

        public WcfManager(IEnumerable<Assembly> assembliesToScan)
        {
            foreach (var a in assembliesToScan)
                foreach (var t in a.GetTypes())
                    if (IsWcfService(t) && !t.IsAbstract)
                        serviceTypes.Add(t);
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

        public void Startup()
        {
            foreach (var serviceType in serviceTypes)
            {
                var host = new WcfServiceHost(serviceType);

                var binding = Configure.Instance.Builder.Build<Binding>();
                if (binding == null)
                    binding = new BasicHttpBinding();

                host.AddDefaultEndpoint(   GetContractType(serviceType),
                                           binding
                                           ,"");

                hosts.Add(host);

                logger.Debug("Going to host the WCF service: " + serviceType.AssemblyQualifiedName);
                host.Open();
            }
        }

        public void Shutdown()
        {
            hosts.ForEach(h => h.Close());
        }

        private static Type GetContractType(Type t)
        {
            var args = t.BaseType.GetGenericArguments();

            return typeof(IWcfService<,>).MakeGenericType(args);
        }


       
        private readonly ILog logger = LogManager.GetLogger(typeof(WcfManager));
    }
}