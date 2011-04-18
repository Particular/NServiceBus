namespace NServiceBus.Gateway.Dispatchers
{
    public interface IDispatchMessagesToChannels
    {
        void Start(string inputAddress);
    }
}