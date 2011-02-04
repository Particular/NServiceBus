namespace NServiceBus.Proxy
{
    public interface IProxyDataStorage
    {
        void Init();
        void Save(ProxyData data);
        ProxyData GetAndRemove(string id);
    }
}
