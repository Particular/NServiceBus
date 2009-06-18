namespace NServiceBus.Host
{
    public interface IMessageEndpointConfiguration
    {
        Configure ConfigureBus(Configure config);   
    }
}