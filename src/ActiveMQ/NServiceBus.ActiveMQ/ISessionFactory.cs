namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;

    public interface ISessionFactory
    {
        INetTxSession GetSession();

        void Release(INetTxSession session);

        void SetSessionForCurrentThread(INetTxSession session);

        void RemoveSessionForCurrentThread();
    }
}