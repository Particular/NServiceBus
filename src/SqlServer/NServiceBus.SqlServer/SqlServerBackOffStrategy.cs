namespace NServiceBus.Transports.SQLServer
{
    using System;
    using System.Threading;

    public abstract class SqlServerBackOffToken
    {
        bool isDisposed = false;

        public virtual void BackOff(CancellationToken cancellationToken)
        {
            // noop
        }

        protected bool IsDisposed { get { return isDisposed; } }

        public virtual void ReceivedMessage()
        {
            // noop
        }

        public void Dispose()
        {
            Dispose(true);
            isDisposed = true;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            
        }
    }

    public interface ISqlServerBackOffStrategy : IDisposable
    {
        SqlServerBackOffToken RegisterWorker();
        void UnRegisterWorker(SqlServerBackOffToken token);
        void Start(string connectionString, string tableName, int maximumConcurrency);
        void Stop();
    }

    public abstract class SqlServerServerBackOffStrategy : ISqlServerBackOffStrategy
    {
        public abstract SqlServerBackOffToken RegisterWorker();

        public virtual void UnRegisterWorker(SqlServerBackOffToken token)
        {
        }

        public virtual void Start(string connectionString, string tableName, int maximumConcurrency)
        {
        }

        public virtual void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }
    }


}