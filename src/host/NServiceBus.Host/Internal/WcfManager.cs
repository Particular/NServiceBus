using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceModel;
using Common.Logging;

namespace NServiceBus.Host
{
    internal class WcfManager
    {
        private readonly List<Type> serviceTypes = new List<Type>();
        private readonly List<ServiceHost> hosts = new List<ServiceHost>();

        public WcfManager(IEnumerable<Assembly> assembliesToScan, IConfigureThisEndpoint specifier)
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
            foreach(var t in serviceTypes)
            {
                var h = new ServiceHost(t);
                hosts.Add(h);

                logger.Debug("Going to host the WCF service: " + t.AssemblyQualifiedName);
                h.Open();
            }
        }

        public void Shutdown()
        {
            hosts.ForEach(h => h.Close());
        }

        private readonly ILog logger = LogManager.GetLogger(typeof(WcfManager));
    }
}
