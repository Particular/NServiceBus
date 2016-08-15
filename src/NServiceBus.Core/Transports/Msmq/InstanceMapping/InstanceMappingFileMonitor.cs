namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using Routing;

    class InstanceMappingFileMonitor : FeatureStartupTask
    {
        public InstanceMappingFileMonitor(string filePath, TimeSpan checkInterval, IAsyncTimer timer, IInstanceMappingFileAccess fileAccess, EndpointInstances endpointInstances)
        {
            this.filePath = filePath;
            this.checkInterval = checkInterval;
            this.timer = timer;
            this.fileAccess = fileAccess;
            this.endpointInstances = endpointInstances;
        }

        protected override Task OnStart(IMessageSession session)
        {
            timer.Start(() =>
            {
                ReloadData();
                return TaskEx.CompletedTask;
            }, checkInterval, ex => log.Error("Unable to update instance mapping information because the instance mapping file couldn't be read.", ex));
            return TaskEx.CompletedTask;
        }

        public void ReloadData()
        {
            try
            {
                var doc = fileAccess.Load(filePath);
                var instances = parser.Parse(doc);
                LogChanges(instances, filePath);
                endpointInstances.AddOrReplaceInstances("InstanceMappingFile", instances);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while reading the endpoint instance mapping file at {filePath}. See the inner exception for more details.", ex);
            }
        }

        void LogChanges(List<EndpointInstance> instances, string filepath)
        {
            var output = new StringBuilder();
            var hasChanges = false;

            var instancesPerEndpoint = instances.GroupBy(i => i.Endpoint).ToDictionary(g => g.Key, g => g.ToArray());

            output.AppendLine($"Updating instance mapping table from '{filepath}':");

            foreach (var endpoint in instancesPerEndpoint)
            {
                EndpointInstance[] existingInstances;
                if (previousInstances.TryGetValue(endpoint.Key, out existingInstances))
                {
                    var newInstances = endpoint.Value.Except(existingInstances).Count();
                    var removedInstances = existingInstances.Except(endpoint.Value).Count();

                    if (newInstances > 0 || removedInstances > 0)
                    {
                        output.AppendLine($"Updated endpoint '{endpoint.Key}': +{Instances(newInstances)}, -{Instances(removedInstances)}");
                        hasChanges = true;
                    }
                }
                else
                {
                    output.AppendLine($"Added endpoint '{endpoint.Key}' with {Instances(endpoint.Value.Length)}");
                    hasChanges = true;
                }
            }

            foreach (var removedEndpoint in previousInstances.Keys.Except(instancesPerEndpoint.Keys))
            {
                output.AppendLine($"Removed all instances of endpoint '{removedEndpoint}'");
                hasChanges = true;
            }

            if (hasChanges)
            {
                log.Info(output.ToString());
            }

            previousInstances = instancesPerEndpoint;
        }

        static string Instances(int count)
        {
            return count > 1 ? $"{count} instances" : $"{count} instance";
        }

        protected override Task OnStop(IMessageSession session) => timer.Stop();

        TimeSpan checkInterval;
        IInstanceMappingFileAccess fileAccess;
        string filePath;
        EndpointInstances endpointInstances;
        InstanceMappingFileParser parser = new InstanceMappingFileParser();
        IAsyncTimer timer;
        IDictionary<string, EndpointInstance[]> previousInstances = new Dictionary<string, EndpointInstance[]>(0);

        static ILog log = LogManager.GetLogger(typeof(InstanceMappingFileMonitor));
    }
}