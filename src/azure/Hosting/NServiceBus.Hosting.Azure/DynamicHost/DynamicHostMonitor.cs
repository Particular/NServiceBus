using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Logging;

namespace NServiceBus.Hosting
{
    internal class DynamicHostMonitor
    {
        private Thread monitorThread;
        private List<EndpointToHost> endpoints = new List<EndpointToHost>();
        private bool stop;
        private readonly ILog logger = LogManager.GetLogger(typeof(DynamicEndpointRunner));

        public DynamicEndpointLoader Loader { get; set; }

        public event EventHandler<EndpointsEventArgs> UpdatedEndpoints;
        public event EventHandler<EndpointsEventArgs> RemovedEndpoints;
        public event EventHandler<EndpointsEventArgs> NewEndpoints;

        public int Interval { get; set; }

        public void OnUpdatedEndpoints(EndpointsEventArgs e)
        {
            var handler = UpdatedEndpoints;
            if (handler != null) handler(this, e);
        }

        public void OnNewEndpoints(EndpointsEventArgs e)
        {
            var handler = NewEndpoints;
            if (handler != null) handler(this, e);
        }

        public void OnRemovedEndpoints(EndpointsEventArgs e)
        {
            var handler = RemovedEndpoints;
            if (handler != null) handler(this, e);
        }

        public void Monitor(IEnumerable<EndpointToHost> hostedEndpoints)
        {
            endpoints.AddRange(hostedEndpoints);
        }

        public void StopMonitoring(IEnumerable<EndpointToHost> hostedEndpoints)
        {
            foreach (var hostedEndpoint in hostedEndpoints)
            {
                endpoints.Remove(hostedEndpoint);
            }
        }

        public void Start()
        {
            stop = false;
            monitorThread = new Thread(() =>{
                while (!stop)
                {
                    Thread.Sleep(Interval);

                    CheckForUpdates();
                }
            });
            monitorThread.Start();
        }

        private void CheckForUpdates()
        {
            try
            {
                var updatedEndpoints = new List<EndpointToHost>();
                var newEndpoints = new List<EndpointToHost>();
                var removedEndpoints = new List<EndpointToHost>();

                var loadedEndpoints = Loader.LoadEndpoints();

                foreach (var loadedEndpoint in loadedEndpoints)
                {
                    var endpoint = endpoints.SingleOrDefault(e => e.EndpointName == loadedEndpoint.EndpointName);
                    if (endpoint == null)
                    {
                        newEndpoints.Add(loadedEndpoint);
                    }
                    else
                    {
                        if (loadedEndpoint.LastUpdated > endpoint.LastUpdated)
                        {
                            endpoint.LastUpdated = loadedEndpoint.LastUpdated;
                            updatedEndpoints.Add(endpoint);
                        }
                    }
                }

                foreach (var endpoint in endpoints)
                {
                    var endpointNotFound =
                        loadedEndpoints.SingleOrDefault(l => l.EndpointName == endpoint.EndpointName) == null;
                    if (endpointNotFound) removedEndpoints.Add(endpoint);
                }

                if (updatedEndpoints.Count > 0)
                    OnUpdatedEndpoints(new EndpointsEventArgs {Endpoints = updatedEndpoints});

                if (newEndpoints.Count > 0)
                    OnNewEndpoints(new EndpointsEventArgs {Endpoints = newEndpoints});

                if (removedEndpoints.Count > 0)
                    OnRemovedEndpoints(new EndpointsEventArgs {Endpoints = removedEndpoints});
            }
            catch(StorageServerException ex) //prevent azure storage hickups from hurting us
            {
                logger.Warn(ex.Message);
            }
        }

        public void Stop()
        {
            stop = true;
        }
    }
}