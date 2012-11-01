namespace NServiceBus.Satellites
{
    public interface ISatellite
    {                
        void Handle(TransportMessage message);
        Address InputAddress { get; }
        bool Disabled { get; }        
        void Start();
        void Stop();
    }
}
