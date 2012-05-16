using NServiceBus.Unicast.Transport;

namespace NServiceBus.Satellites
{
    public interface ISatellite
    {                
        void Handle(TransportMessage message);
        Address InputAddress { get; set; }
        bool Disabled { get; set; }        
        void Start();
        void Stop();
    }
}
