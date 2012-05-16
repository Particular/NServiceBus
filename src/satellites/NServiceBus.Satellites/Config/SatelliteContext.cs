using System;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Satellites.Config
{
    public class SatelliteContext
    {
        internal SatelliteContext()
        {
            FailedAttempts = 0;
            Instance = null;
            Started = false;
            Transport = null;            
            Enabled = false;
        }

        public Type TypeOfSatellite { get; set; }
        public ITransport Transport { get; set; }
        public ISatellite Instance { get; set; }
        public int FailedAttempts { get; set; }
        public bool Started { get; set; }
        public int NumberOfWorkerThreads { get; set; }
        public int MaxRetries { get; set; }
        public bool IsTransactional { get; set; }
        public bool Enabled { get; set; }
    }
}