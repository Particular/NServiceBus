namespace NServiceBus.Satellites.Config
{
    using Unicast.Transport;

    class SatelliteContext
    {
        internal SatelliteContext()
        {
            Instance = null;
            Transport = null;                        
        }

        public TransportReceiver Transport { get; set; }
        public ISatellite Instance { get; set; }
    }
}