namespace NServiceBus.Transports.SQLServer
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Threading;
    using Logging;

    public class SqlServerDependencyBackOffToken : SqlServerBackOffToken
    {
        readonly Action resetBackOff;
        readonly Action<CancellationToken> waitForMessage;
        readonly Action<SqlServerDependencyBackOffToken> unregisterAction;
        static readonly int[] retryDelays = new[] { 0, 0, 0, 10, 10, 10, 50, 50, 100, 100, 200, 200, 200, 200 };
        int currentRetryCount = 0;

        public SqlServerDependencyBackOffToken(Action resetBackOff, Action<CancellationToken> waitForMessage, Action<SqlServerDependencyBackOffToken> unregisterAction)
        {
            this.resetBackOff = resetBackOff;
            this.waitForMessage = waitForMessage;
            this.unregisterAction = unregisterAction;
        }

        public override void ReceivedMessage()
        {
            var h = resetBackOff;
            if (h != null)
            {
                h();
            }
            currentRetryCount = 0;
        }

        public override void BackOff(CancellationToken token)
        {
            if (currentRetryCount >= retryDelays.Length)
            {
                var h = waitForMessage;
                if (h != null)
                {
                    currentRetryCount = 0;
                    h(token);
                }
            }
            else
            {
                Thread.Sleep(retryDelays[currentRetryCount++]);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing && !IsDisposed)
            {
                var h = unregisterAction;
                if (h != null)
                {
                    h(this);
                }
            }
        }
    }

    public class SqlServerDependencyBackOffStrategy : SqlServerBackOffStrategy
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SqlServerDependencyBackOffStrategy));

        Lazy<object> sqlDependencyInit;

        ManualResetEventSlim waitForSqlDependency;
        CountdownEvent signalSqlDependency;
        RegisteredWaitHandle registeredSignalSqlDependency;
        int registeredWorkers = 0;
        SpinLock workerLocker = new SpinLock();
        SqlCommand receiveCommand;

        public SqlServerDependencyBackOffStrategy()
        {
            sqlDependencyInit = new Lazy<object>(InitSqlDependency);
        }

        public string ConnectionString { get; set; }

        public string TableName { get; set; }

        public override SqlServerBackOffToken RegisterWorker()
        {
            AddWorker();
            return new SqlServerDependencyBackOffToken(ReceivedMessage, WaitForMessage, UnRegisterWorker);
        }

        public override void UnRegisterWorker(SqlServerBackOffToken token)
        {
            RemoveWorker();
        }

        public override void Start(string connectionString, string tableName, int maximumConcurrency)
        {
            ConnectionString = connectionString;
            TableName = tableName;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {

                if (sqlDependencyInit.IsValueCreated)
                {
                    SqlDependency.Stop(ConnectionString);

                    // Reset the lazy in case we get started again after we were disposed
                    sqlDependencyInit = new Lazy<object>(InitSqlDependency);
                }

                if (waitForSqlDependency != null)
                {
                    waitForSqlDependency.Dispose();
                    waitForSqlDependency = null;
                }

                if (signalSqlDependency != null)
                {
                    if (registeredSignalSqlDependency != null)
                    {
                        registeredSignalSqlDependency.Unregister(signalSqlDependency.WaitHandle);
                        registeredSignalSqlDependency = null;
                    }

                    signalSqlDependency.Dispose();
                    signalSqlDependency = null;
                }

                if (receiveCommand != null)
                {
                    try
                    {
                        receiveCommand.Cancel();
                    }
                    catch (Exception) { }

                    try
                    {
                        receiveCommand.Dispose();
                    }
                    catch (Exception) { }

                    receiveCommand = null;
                }

                registeredWorkers = 0;
            }
        }

        void AddWorker()
        {
            bool gotLock = false;
            try
            {
                workerLocker.Enter(ref gotLock);
                registeredWorkers += 1;
                if (waitForSqlDependency == null)
                {
                    waitForSqlDependency = new ManualResetEventSlim(true);
                }

                if (signalSqlDependency == null)
                {
                    signalSqlDependency = new CountdownEvent(registeredWorkers);
                    registeredSignalSqlDependency =
                        ThreadPool.RegisterWaitForSingleObject(
                            signalSqlDependency.WaitHandle, SetupQueryNotification, this, -1, true);
                }
                else
                {
                    if (!signalSqlDependency.TryAddCount(1))
                    {
                        // TODO: What do we do here?
                        throw new InvalidOperationException(
                            "Error adding another thread to the sql dependency count down event, something terriable must have gone wrong.");
                    }
                }
            }
            finally
            {
                if (gotLock)
                {
                    workerLocker.Exit();
                }
            }
        }

        void RemoveWorker()
        {
            bool gotLock = false;
            try
            {
                workerLocker.Enter(ref gotLock);

                registeredWorkers -= 1;
                if (registeredWorkers == 0)
                {
                    if (signalSqlDependency != null)
                    {
                        if (registeredSignalSqlDependency != null)
                        {
                            registeredSignalSqlDependency.Unregister(signalSqlDependency.WaitHandle);
                        }
                        signalSqlDependency.Dispose();
                        signalSqlDependency = null;
                    }
                }
                else
                {
                    Debug.Assert(
                        signalSqlDependency != null,
                        "Sql Dependency Count Down Event should never be null if worker count is greater than 0");

                    signalSqlDependency.Signal();
                }
            }
            finally
            {
                if (gotLock)
                {
                    workerLocker.Exit();
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals",
            MessageId = "dummy", Justification = "Dummy value returned from lazy init routine.")]
        void SetupQueryNotification(object state, bool timeout)
        {
            Logger.DebugFormat("Setting up SQL notification for table {0}", TableName);

            //if (registeredSignalSqlDependency != null)
            //{
            //    registeredSignalSqlDependency.Unregister(signalSqlDependency.WaitHandle);
            //    registeredSignalSqlDependency = null;
            //}

            var dummy = sqlDependencyInit.Value;
            using (var connection = new SqlConnection(ConnectionString))
            {
                if (receiveCommand == null)
                {
                    receiveCommand = connection.CreateCommand();
                    receiveCommand.CommandText = "SELECT s.Id as Id FROM [dbo].[" + TableName + "] as s";
                }
                receiveCommand.Notification = null;
                var sqlDependency = new SqlDependency(receiveCommand);
                OnChangeEventHandler handler = (sender, e) =>
                {
                    Logger.InfoFormat("SqlDependency.OnChanged fired in table {1}: {0}", e.Info, TableName);
                    receiveCommand.Notification = null;

                    if (e.Type == SqlNotificationType.Change)
                    {
                        // unleash the workers
                        waitForSqlDependency.Set();
                    }
                    else
                    {
                        // If the e.Info value here is 'Invalid', ensure the query SQL meets the requirements
                        // for query notifications at http://msdn.microsoft.com/en-US/library/ms181122.aspx
                        Logger.ErrorFormat("SQL notification subscription error: {0}", e.Info);

                        // TODO: Do we need to be more paticular about the type of error here?
                        waitForSqlDependency.Set();
                    }
                };

                sqlDependency.OnChange += handler;
                receiveCommand.Connection = connection;
                connection.Open();

                // Executing the query is required to set up the dependency
                using (var reader = this.receiveCommand.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (reader.HasRows)
                    {
                        // If we have rows then we can stop listening, and signal our workers to dequeue
                        sqlDependency.OnChange -= handler;
                        receiveCommand.Notification = null;
                        waitForSqlDependency.Set();
                    }
                }

                // Reset the countdown event so all threads will have to finish before triggering a new sqldependency
                bool gotLock = false;
                try
                {
                    workerLocker.Enter(ref gotLock);
                    signalSqlDependency.Reset(registeredWorkers);
                }
                finally
                {
                    if (gotLock)
                    {
                        workerLocker.Exit();
                    }
                }

                // RegisterWorker this callback again in the threadpool to be fired when the signalSqlDependency is Set
                registeredSignalSqlDependency = ThreadPool.RegisterWaitForSingleObject(
                    signalSqlDependency.WaitHandle, SetupQueryNotification, this, -1, true);

                Logger.DebugFormat("SQL notification set up for {0}", TableName);
            }
        }

        object InitSqlDependency()
        {
            Logger.DebugFormat("Starting SQL notification listener for {0}", ConnectionString);

            var perm = new SqlClientPermission(PermissionState.Unrestricted);
            perm.Demand();

            SqlDependency.Start(this.ConnectionString);

            Logger.DebugFormat("SQL notification listener started for {0}", ConnectionString);
            return new object();
        }

        void WaitForMessage(CancellationToken token)
        {
            Logger.DebugFormat("SQL notification listener {0} waiting for message", TableName);
            waitForSqlDependency.Reset();
            signalSqlDependency.Signal();
            WaitHandle.WaitAny(new[] { token.WaitHandle, waitForSqlDependency.WaitHandle });
        }

        void ReceivedMessage()
        {
            bool gotLock = false;
            try
            {
                workerLocker.Enter(ref gotLock);
                signalSqlDependency.Reset(registeredWorkers);
                waitForSqlDependency.Set();
            }
            finally
            {
                if (gotLock)
                {
                    workerLocker.Exit();
                }
            }
        }
    }
}