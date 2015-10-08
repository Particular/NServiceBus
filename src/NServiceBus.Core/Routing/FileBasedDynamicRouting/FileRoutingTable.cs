namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Janitor;
    using NServiceBus.Logging;

    [SkipWeaving]
    class FileRoutingTable : IDisposable
    {
        public FileRoutingTable(string basePath, TimeSpan timeToWaitBeforeRaisingFileChangedEvent)
        {
            this.basePath = basePath;
            this.timeToWaitBeforeRaisingFileChangedEvent = timeToWaitBeforeRaisingFileChangedEvent;
        }

        public IEnumerable<EndpointInstanceName> GetInstances(EndpointName endpointName)
        {
            logger.DebugFormat("Request routes for {0}.", endpointName);

            CacheRoute routes;
            if (!routeMapping.TryGetValue(endpointName.ToString(), out routes))
            {
                UpdateMapping(endpointName.ToString(), true);

                if (!routeMapping.TryGetValue(endpointName.ToString(), out routes))
                {
                    yield break;
                }
            }
            foreach (var route in routes.Routes)
            {
                var discriminators = route.Split(new []{':'},StringSplitOptions.None);
                if (discriminators.Length == 2)
                {
                    var userDiscriminator = NullIfEmptyString(discriminators[0].Trim());
                    var transportDiscriminator = NullIfEmptyString(discriminators[1].Trim());

                    yield return new EndpointInstanceName(endpointName, userDiscriminator, transportDiscriminator);
                }
                else
                {
                    logger.Info($"Invalid route {route}. Expecting <userDiscriminator>:<transportDiscriminator> format");
                }
            }
        }

        static string NullIfEmptyString(string value)
        {
            return value.Equals("", StringComparison.InvariantCultureIgnoreCase) 
                ? null 
                : value;
        }

        void StartMonitoring(string basePath, string queueName, string fileName)
        {
            monitoringFiles.Add(new MonitorFileChanges(basePath, queueName, fileName, timeToWaitBeforeRaisingFileChangedEvent, s => UpdateMapping(s, false)));
        }

        void UpdateMapping(string queueName, bool startMonitor)
        {
            try
            {
                if (Monitor.TryEnter(string.Intern(queueName)))
                {
                    var fileName = $"{queueName}.txt";
                    var filePath = Path.Combine(basePath, fileName);

                    logger.InfoFormat("Refreshing routes for '{0}' from '{1}'", queueName, filePath);

                    if (startMonitor)
                    {
                        logger.InfoFormat("Monitoring '{0}' for changes.", queueName);

                        StartMonitoring(basePath, queueName, fileName);
                    }

                    if (!File.Exists(filePath))
                    {
                        logger.DebugFormat("No file found for '{0}'.", queueName);

                        routeMapping[queueName] = new CacheRoute(new string[0]);
                        return;
                    }

                    logger.DebugFormat("Reading '{0}' file.", fileName);

                    routeMapping[queueName] = new CacheRoute(ReadAllLinesWithoutLocking(filePath).ToArray());

                    logger.DebugFormat("Routing updated for {0}.", queueName);
                }

            }
            finally
            {
                Monitor.Exit(string.Intern(queueName));
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

        string basePath;
        readonly TimeSpan timeToWaitBeforeRaisingFileChangedEvent;
        static ILog logger = LogManager.GetLogger<FileRoutingTable>();
        List<MonitorFileChanges> monitoringFiles = new List<MonitorFileChanges>();
        ConcurrentDictionary<string, CacheRoute> routeMapping = new ConcurrentDictionary<string, CacheRoute>();

        class CacheRoute
        {
            public CacheRoute(string[] routes)
            {
                Routes = routes;
            }

            public string[] Routes { get; private set; }
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