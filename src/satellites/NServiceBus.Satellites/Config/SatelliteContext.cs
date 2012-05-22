﻿using NServiceBus.Unicast.Transport;

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
        }
        
        public ITransport Transport { get; set; }
        public ISatellite Instance { get; set; }
        public int FailedAttempts { get; set; }
        public bool Started { get; set; }        
    }
}