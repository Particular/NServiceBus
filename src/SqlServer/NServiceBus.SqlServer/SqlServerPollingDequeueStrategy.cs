namespace NServiceBus.Transports.SQLServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using System.Transactions;
    using Logging;
    using Serializers.Json;
    using Utils;
    using Unicast.Transport;
    using IsolationLevel = System.Data.IsolationLevel;

    /// <summary>
    ///     A polling implementation of <see cref="IDequeueMessages" />.
    /// </summary>
    public class SqlServerPollingDequeueStrategy : IDequeueMessages
    {
        private const string SqlReceive =
            @"WITH message AS (SELECT TOP(1) * FROM [{0}] WITH (UPDLOCK, READPAST) ORDER BY TimeStamp ASC) 
			DELETE FROM message 
			OUTPUT deleted.Id, deleted.CorrelationId, deleted.ReplyToAddress, 
			deleted.Recoverable, deleted.Expires, deleted.Headers, deleted.Body;";
        private const string SqlPurge = @"DELETE FROM [{0}]";

        private static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SqlServerPollingDequeueStrategy));

        private readonly CircuitBreaker circuitBreaker = new CircuitBreaker(100, TimeSpan.FromSeconds(30));

        private Address addressToPoll;
        private Action<string, Exception> endProcessMessage;
        private MTATaskScheduler scheduler;
        private TransactionSettings settings;
        private string sql;
        private string tableName;
        private CancellationTokenSource tokenSource;
        private TransactionOptions transactionOptions;
        private Func<TransportMessage, bool> tryProcessMessage;
        private SqlCommand receiveCommand;
        private Lazy<object> sqlDependencyInit;
        private readonly int[] retryDelays = new[] { 0, 0, 0, 10, 10, 10, 50, 50, 100, 100, 200, 200, 200, 200 };
        ManualResetEventSlim waitForSqlDependency;
        WaitHandle[] waitHandles;
        CountdownEvent signalSqlDependency;
        RegisteredWaitHandle registeredSignalSqlDependency;

        /// <summary>
        ///     The connection used to open the SQL Server database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Determines if the queue should be purged when the transport starts.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        /// <summary>
        /// SqlServer <see cref="ISendMessages"/>.
        /// </summary>
        public SqlServerMessageSender MessageSender { get; set; }
        
        /// <summary>
        ///     Initializes the <see cref="IDequeueMessages" />.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="transactionSettings">
        ///     The <see cref="TransactionSettings" /> to be used by <see cref="IDequeueMessages" />.
        /// </param>
        /// <param name="tryProcessMessage">Called when a message has been dequeued and is ready for processing.</param>
        /// <param name="endProcessMessage">
        ///     Needs to be called by <see cref="IDequeueMessages" /> after the message has been processed regardless if the outcome was successful or not.
        /// </param>
        public void Init(Address address, TransactionSettings transactionSettings,
                         Func<TransportMessage, bool> tryProcessMessage, Action<string, Exception> endProcessMessage)
        {
            sqlDependencyInit = new Lazy<object>(InitSqlDependency);
            this.tryProcessMessage = tryProcessMessage;
            this.endProcessMessage = endProcessMessage;

            addressToPoll = address;
            settings = transactionSettings;
            transactionOptions = new TransactionOptions
                {
                    IsolationLevel = transactionSettings.IsolationLevel,
                    Timeout = transactionSettings.TransactionTimeout
                };

            tableName = address.Queue;

            sql = string.Format(SqlReceive, tableName);

            if (PurgeOnStartup)
            {
                PurgeTable();
            }
        }

        /// <summary>
        ///     Starts the dequeuing of message using the specified <paramref name="maximumConcurrencyLevel" />.
        /// </summary>
        /// <param name="maximumConcurrencyLevel">
        ///     Indicates the maximum concurrency level this <see cref="IDequeueMessages" /> is able to support.
        /// </param>
        public void Start(int maximumConcurrencyLevel)
        {
            tokenSource = new CancellationTokenSource();
            waitForSqlDependency = new ManualResetEventSlim(true);
            signalSqlDependency = new CountdownEvent(maximumConcurrencyLevel);
            waitHandles = new[] { tokenSource.Token.WaitHandle, waitForSqlDependency.WaitHandle };

            scheduler = new MTATaskScheduler(maximumConcurrencyLevel,
                                             String.Format("NServiceBus Dequeuer Worker Thread for [{0}]", addressToPoll));

            for (int i = 0; i < maximumConcurrencyLevel; i++)
            {
                StartThread();
            }

            registeredSignalSqlDependency = ThreadPool.RegisterWaitForSingleObject(signalSqlDependency.WaitHandle, SetupQueryNotification, this, -1, true);
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();
            scheduler.Dispose();

            SqlDependency.Stop(ConnectionString);
            waitForSqlDependency.Dispose();
            waitForSqlDependency = null;

            if (registeredSignalSqlDependency != null)
            {
                registeredSignalSqlDependency.Unregister(signalSqlDependency.WaitHandle);
                registeredSignalSqlDependency = null;
            }

            signalSqlDependency.Dispose();
            signalSqlDependency = null;
        }

        private void PurgeTable()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(string.Format(SqlPurge, tableName), connection)
                        {
                            CommandType = CommandType.Text
                        })
                {
                    int numberOfPurgedRows = command.ExecuteNonQuery();

                    Logger.InfoFormat("{0} messages was purged from table {1}", numberOfPurgedRows, tableName);
                }
            }
        }

        private void StartThread()
        {
            CancellationToken token = tokenSource.Token;

            Task.Factory
                .StartNew(Action, token, token, TaskCreationOptions.None, scheduler)
                .ContinueWith(t =>
                    {
                        t.Exception.Handle(ex =>
                            {
                                circuitBreaker.Execute(
                                    () =>
                                    Configure.Instance.RaiseCriticalError(
                                        string.Format("Failed to receive message from '{0}'.", tableName), ex));
                                return true;
                            });

                        StartThread();
                    }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void Action(object obj)
        {
            var cancellationToken = (CancellationToken) obj;

            while (!cancellationToken.IsCancellationRequested)
            {
                Exception exception = null;
                TransportMessage message = null;

                for (int i = 0; i < retryDelays.Length && !cancellationToken.IsCancellationRequested; i++)
                {
                    try
                    {
                        if (settings.IsTransactional)
                        {
                            if (settings.DontUseDistributedTransactions)
                            {
                                message = TryReceiveWithNativeTransaction();
                            }
                            else
                            {
                                message = TryReceiveWithDTCTransaction();
                            }
                        }
                        else
                        {
                            message = TryReceiveWithNoTransaction();
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    finally
                    {
                        if (message != null)
                        {
                            // reset our backoff since we got a message
                            i = 0;

                            // unleash all workers that might have been waiting since we got message
                            waitForSqlDependency.Set();

                            // make sure to reset the counter that triggers the sql dependency registration
                            signalSqlDependency.Reset();
                        }

                        // send the message off to nsb for processing
                        endProcessMessage(message != null ? message.Id : null, exception);
                    }
                }

                // Have workers block until the waitForSqlDependency is triggered
                waitForSqlDependency.Reset();

                // Decrement the counter that will trigger the sql dependency to be registered
                signalSqlDependency.Signal();

                // This thread will wait until the cancellation token is signaled or the their are messages
                WaitHandle.WaitAny(waitHandles);
            }
        }

        TransportMessage TryReceiveWithNoTransaction()
        {
            var message = Receive();

            if (message != null)
            {
                tryProcessMessage(message);
            }
            return message;
        }

        TransportMessage TryReceiveWithDTCTransaction()
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                var message = Receive();

                if (message == null)
                {
                    scope.Complete();
                    return null;
                }

                if (tryProcessMessage(message))
                {
                    scope.Complete();
                }

                return message;
            }
           
        }

        TransportMessage TryReceiveWithNativeTransaction()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    circuitBreaker.Execute(
                                    () =>
                                    Configure.Instance.RaiseCriticalError("Failed to open a sql connection.", ex));

                    throw;
                }

                using (var transaction = connection.BeginTransaction(GetSqlIsolationLevel(settings.IsolationLevel)))
                {
                    TransportMessage message;
                    try
                    {
                        message = ReceiveWithNativeTransaction(connection, transaction);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        circuitBreaker.Execute(
                                        () =>
                                        Configure.Instance.RaiseCriticalError(
                                            string.Format("Failed to receive message from '{0}'.", tableName), ex));

                        throw;
                    }

                    if (message == null)
                    {
                        transaction.Commit();
                        return null;
                    }

                    if (MessageSender != null)
                    {
                        MessageSender.SetTransaction(transaction);
                    }

                    if (tryProcessMessage(message))
                    {
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.Rollback();
                    }

                    return message;
                }
            }
          
        }

        private TransportMessage Receive()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text })
                    {
                        return ExecuteReader(command);
                    }
                }
            }
            catch (Exception ex)
            {
                circuitBreaker.Execute(
                                    () =>
                                    Configure.Instance.RaiseCriticalError(
                                        string.Format("Failed to receive message from '{0}'.", tableName), ex));
                throw;
            }
        }

        private TransportMessage ReceiveWithNativeTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(sql, connection, transaction) { CommandType = CommandType.Text })
            {
                return ExecuteReader(command);
            }
        }

        private static TransportMessage ExecuteReader(SqlCommand command)
        {
            using (var dataReader = command.ExecuteReader(CommandBehavior.SingleRow))
            {
                if (dataReader.Read())
                {
                    var id = dataReader.GetGuid(0).ToString();
                   
                    DateTime? expireDateTime = null;
                    if (!dataReader.IsDBNull(4))
                    {
                        expireDateTime = dataReader.GetDateTime(4);
                    }

                    //Has message expired?
                    if (expireDateTime.HasValue && expireDateTime.Value < DateTime.UtcNow)
                    {
                        Logger.InfoFormat("Message with ID={0} has expired. Removing it from queue.", id);
                        return null;
                    }

                    var headers = Serializer.DeserializeObject<Dictionary<string, string>>(dataReader.GetString(5));
                    var correlationId = dataReader.IsDBNull(1) ? null : dataReader.GetString(1);
                    var recoverable = dataReader.GetBoolean(3);
                    var body = dataReader.IsDBNull(6) ? null : dataReader.GetSqlBinary(6).Value;
                    var replyToAddress = dataReader.IsDBNull(2) ? null : Address.Parse(dataReader.GetString(2));

                    var message = new TransportMessage
                        {
                            Id = id,
                            CorrelationId = correlationId,
                            ReplyToAddress = replyToAddress,
                            Recoverable = recoverable,
                            Headers = headers,
                            Body = body
                        };

                    if (expireDateTime.HasValue)
                    {
                        message.TimeToBeReceived = TimeSpan.FromTicks(expireDateTime.Value.Ticks - DateTime.UtcNow.Ticks);
                    }

                    return message;
                }
            }

            return null;
        }

        private IsolationLevel GetSqlIsolationLevel(System.Transactions.IsolationLevel isolationLevel)
        {
            switch (isolationLevel)
            {
                case System.Transactions.IsolationLevel.Serializable:
                    return IsolationLevel.Serializable;
                case System.Transactions.IsolationLevel.RepeatableRead:
                    return IsolationLevel.RepeatableRead;
                case System.Transactions.IsolationLevel.ReadCommitted:
                    return IsolationLevel.ReadCommitted;
                case System.Transactions.IsolationLevel.ReadUncommitted:
                    return IsolationLevel.ReadUncommitted;
                case System.Transactions.IsolationLevel.Snapshot:
                    return IsolationLevel.Snapshot;
                case System.Transactions.IsolationLevel.Chaos:
                    return IsolationLevel.Chaos;
                case System.Transactions.IsolationLevel.Unspecified:
                    return IsolationLevel.Unspecified;
            }

            return IsolationLevel.ReadCommitted;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "dummy", Justification = "Dummy value returned from lazy init routine.")]
        private void SetupQueryNotification(object state, bool timeout)
        {
            Logger.Debug("Setting up SQL notification");

            var dummy = sqlDependencyInit.Value;
            using (var connection = new SqlConnection(ConnectionString))
            {
                if (receiveCommand == null)
                {
                    receiveCommand = connection.CreateCommand();
                    receiveCommand.CommandText = "SELECT s.Id as Id FROM [dbo].[" + tableName + "] as s";
                }
                var sqlDependency = new SqlDependency(receiveCommand);
                OnChangeEventHandler handler = (sender, e) =>
                {
                    Logger.InfoFormat("SqlDependency.OnChanged fired: {0}", e.Info);
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
                signalSqlDependency.Reset();

                // Register this callback again in the threadpool to be fired when the signalSqlDependency is Set
                registeredSignalSqlDependency = ThreadPool.RegisterWaitForSingleObject(signalSqlDependency.WaitHandle, SetupQueryNotification, this, -1, true);

                Logger.Debug("SQL notification set up");
            }
        }

        private object InitSqlDependency()
        {
            Logger.Debug("Starting SQL notification listener");

            var perm = new SqlClientPermission(PermissionState.Unrestricted);
            perm.Demand();

            SqlDependency.Start(this.ConnectionString);

            Logger.Debug("SQL notification listener started");
            return new object();
        }
    }
}