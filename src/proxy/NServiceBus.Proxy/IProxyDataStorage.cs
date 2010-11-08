namespace NServiceBus.Proxy
{
    public interface IProxyDataStorage
    {
        void Save(ProxyData data);
        ProxyData GetAndRemove(string id);
    }
}
