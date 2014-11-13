namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Logging;

    [SkipWeaving]
    class FileBasedRoundRobinRoutingDistributor : IRouterDistributor, IDisposable
    {
        public FileBasedRoundRobinRoutingDistributor(string basePath, TimeSpan timeToWaitBeforeRaisingFileChangedEvent)
        {
            this.basePath = basePath;
            this.timeToWaitBeforeRaisingFileChangedEvent = timeToWaitBeforeRaisingFileChangedEvent;
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
            Task.Factory.StartNew(() => UpdateMapping(queueName, true));
        }

        void StartMonitoring(string basePath, string queueName, string fileName)
        {
            monitoringFiles.Add(new MonitorFileChanges(basePath, queueName, fileName, timeToWaitBeforeRaisingFileChangedEvent, s => UpdateMapping(s, false)));
        }

        void UpdateMapping(string queueName, bool startMonitor)
        {
            if (Monitor.TryEnter(String.Intern(queueName)))
            {
                var fileName = String.Format("{0}.txt", queueName);
                var filePath = Path.Combine(basePath, fileName);

                logger.InfoFormat("Refreshing routes for '{0}' from '{1}'", queueName, filePath);

                if (startMonitor)
                {
                    logger.InfoFormat("Monitoring '{0}' for changes.", queueName, filePath);

                    StartMonitoring(basePath, queueName, fileName);
                }

                if (!File.Exists(filePath))
                {
                    routeMapping[queueName] = new CacheRoute(new string[0]);
                    return;
                }

                routeMapping[queueName] = new CacheRoute(ReadAllLinesWithoutLocking(filePath).ToArray());
            }
        }

        static IEnumerable<string> ReadAllLinesWithoutLocking(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var textReader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = textReader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var monitoringFile in monitoringFiles)
            {
                monitoringFile.Dispose();
            }
        }

        readonly string basePath;
        readonly TimeSpan timeToWaitBeforeRaisingFileChangedEvent;
        ILog logger = LogManager.GetLogger<FileBasedRoundRobinRoutingDistributor>();
        List<MonitorFileChanges> monitoringFiles = new List<MonitorFileChanges>();
        ConcurrentDictionary<string, CacheRoute> routeMapping = new ConcurrentDictionary<string, CacheRoute>();

        class CacheRoute
        {
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

            readonly string[] routes;
            int index;
            object lockObj = new object();
        }

        class MonitorFileChanges : IDisposable
        {
            readonly TimeSpan toWaitBeforeRaisingFileChangedEvent;

            public MonitorFileChanges(string basePath, string queueName, string fileName, TimeSpan toWaitBeforeRaisingFileChangedEvent, Action<string> update)
            {
                this.toWaitBeforeRaisingFileChangedEvent = toWaitBeforeRaisingFileChangedEvent;
                delayUpdate = new Timer(_ => update(queueName));

                watcher = new FileSystemWatcher(basePath, fileName)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                };
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                watcher.EnableRaisingEvents = true;
            }

            public void Dispose()
            {
                // Injected
            }

            void OnRenamed(object sender, RenamedEventArgs e)
            {
                SetupTimer();
            }

            void OnChanged(object sender, FileSystemEventArgs e)
            {
                SetupTimer();
            }

            void SetupTimer()
            {
                delayUpdate.Change(toWaitBeforeRaisingFileChangedEvent, Timeout.InfiniteTimeSpan);
            }

            Timer delayUpdate;
// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
            FileSystemWatcher watcher;
        }
    }
}