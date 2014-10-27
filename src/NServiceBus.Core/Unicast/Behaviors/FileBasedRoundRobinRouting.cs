namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Logging;

    class FileBasedRoundRobinRouting
    {
        readonly string basePath;
        ConcurrentDictionary<string, CacheRoute> routeMapping = new ConcurrentDictionary<string, CacheRoute>();
        ILog logger = LogManager.GetLogger<FileBasedRoundRobinRouting>();

        public FileBasedRoundRobinRouting(string basePath)
        {
            this.basePath = basePath;
        }

        public bool TryGetRouteAddress(string queueName, out string address)
        {
            address = null;

            CacheRoute routes;
            if (!routeMapping.TryGetValue(queueName, out routes))
            {
                ReadFileAsync(queueName);

                if (!routeMapping.TryGetValue(queueName, out routes))
                {
                    return false;
                }
            }

            if (!routes.TryGetRouteAddress(out address))
            {
                return false;
            }
            
            return true;
        }

        void ReadFileAsync(string queueName)
        {
            if(Monitor.TryEnter(String.Intern(queueName), TimeSpan.FromSeconds(1)))
            {
                if (routeMapping.ContainsKey(queueName))
                {
                    return;
                }

                //Todo: Commenting out reading file in a different thread for now
                //Task.Factory.StartNew(() =>
                //{
                    var filePath = Path.Combine(basePath, String.Format("{0}.txt", queueName));

                    logger.InfoFormat("Attempting to read routes from '{0}'", filePath);
                    
                    if (!File.Exists(filePath))
                    {
                        routeMapping[queueName] = new CacheRoute(new string[0]);
                        return;
                    }

                    routeMapping[queueName] = new CacheRoute(ReadAllLinesWithoutLocking(filePath).ToArray());
                //});
            }
        }

        static IEnumerable<string> ReadAllLinesWithoutLocking(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var textReader = new StreamReader(fileStream))
            {
                string line;
                while ( (line = textReader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        class CacheRoute
        {
            readonly string[] routes;
            int index;
            object lockObj = new object();

            public CacheRoute(string[] routes)
            {
                this.routes = routes;
            }

            public bool TryGetRouteAddress(out string address)
            {
                address = null;

                if (routes.Length == 0)
                {
                    return false;
                }

                lock (lockObj)
                {
                    if (index >= routes.Length)
                    {
                        index = 0;
                    }

                    address = routes[index];

                    index++;
                }

                return true;
            }
        }
    }
}