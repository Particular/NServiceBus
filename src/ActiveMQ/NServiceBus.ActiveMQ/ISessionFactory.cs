namespace NServiceBus.Transport.ActiveMQ
{
    using Apache.NMS;

    public interface ISessionFactory
    {
        INetTxSession GetSession();

        void Release(INetTxSession session);

        void SetSessionForCurrentTransaction(INetTxSession session);

        void RemoveSessionForCurrentTransaction();
    }
}