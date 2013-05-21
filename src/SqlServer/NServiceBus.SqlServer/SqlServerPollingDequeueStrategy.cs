namespace NServiceBus.Transports.SQLServer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Schedulers;
    using System.Transactions;
    using CircuitBreakers;
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
                         Func<TransportMessage, bool> tryProcessMessage, Action<TransportMessage, Exception> endProcessMessage)
        {
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

            scheduler = new MTATaskScheduler(maximumConcurrencyLevel,
                                             String.Format("NServiceBus Dequeuer Worker Thread for [{0}]", addressToPoll));

            for (int i = 0; i < maximumConcurrencyLevel; i++)
            {
                StartThread();
            }
        }

        /// <summary>
        ///     Stops the dequeuing of messages.
        /// </summary>
        public void Stop()
        {
            tokenSource.Cancel();
            scheduler.Dispose();
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
                                Logger.Warn("Failed to connect to the configured SqlServer");
                                circuitBreaker.Failure(ex);
                                return true;
                            });

                        StartThread();
                    }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void Action(object obj)
        {
            var cancellationToken = (CancellationToken)obj;
            var backOff = new BackOff(1000);

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = new ReceiveResult();

                try
                {
                    if (settings.IsTransactional)
                    {
                        if (settings.DontUseDistributedTransactions)
                        {
                            result = TryReceiveWithNativeTransaction();
                        }
                        else
                        {
                            result = TryReceiveWithDTCTransaction();
                        }
                    }
                    else
                    {
                        result = TryReceiveWithNoTransaction();
                    }
                }
                finally
                {
                    //since we're polling the message will be null when there was nothing in the queue
                    if (result.Message != null)
                        endProcessMessage(result.Message, result.Exception);
                }

                circuitBreaker.Success();
                backOff.Wait(() => result.Message == null);
            }
        }

        ReceiveResult TryReceiveWithNoTransaction()
        {
            var result = new ReceiveResult();

            var message = Receive();

            if (message == null)
                return result;

            result.Message = message;
            try
            {
                tryProcessMessage(message);

            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }

            return result;
        }

        ReceiveResult TryReceiveWithDTCTransaction()
        {
            var result = new ReceiveResult();

            using (var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                var message = Receive();

                if (message == null)
                {
                    scope.Complete();
                    return result;
                }

                result.Message = message;

                try
                {
                    if (tryProcessMessage(message))
                    {
                        scope.Complete();
                    }
                }
                catch (Exception ex)
                {
                    result.Exception = ex;
                }

                return result;
            }

        }

        ReceiveResult TryReceiveWithNativeTransaction()
        {
            var result = new ReceiveResult();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(GetSqlIsolationLevel(settings.IsolationLevel)))
                {
                    TransportMessage message;
                    try
                    {
                        message = ReceiveWithNativeTransaction(connection, transaction);
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }

                    if (message == null)
                    {
                        transaction.Commit();
                        return result;
                    }

                    result.Message = message;

                    try
                    {
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

                    }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                        transaction.Rollback();
                    }

                    return result;
                }
            }

        }

        TransportMessage Receive()
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

        TransportMessage ReceiveWithNativeTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(sql, connection, transaction) { CommandType = CommandType.Text })
            {
                return ExecuteReader(command);
            }
        }

        static TransportMessage ExecuteReader(SqlCommand command)
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

                    var message = new TransportMessage(id,headers)
                        {
                            CorrelationId = correlationId,
                            ReplyToAddress = replyToAddress,
                            Recoverable = recoverable,
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

        IsolationLevel GetSqlIsolationLevel(System.Transactions.IsolationLevel isolationLevel)
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

        class ReceiveResult
        {
            public Exception Exception { get; set; }
            public TransportMessage Message { get; set; }
        }

        const string SqlReceive =
         @"WITH message AS (SELECT TOP(1) * FROM [{0}] WITH (UPDLOCK, READPAST, ROWLOCK) ORDER BY [RowVersion] ASC) 
			DELETE FROM message 
			OUTPUT deleted.Id, deleted.CorrelationId, deleted.ReplyToAddress, 
			deleted.Recoverable, deleted.Expires, deleted.Headers, deleted.Body;";
        const string SqlPurge = @"DELETE FROM [{0}]";

        static readonly JsonMessageSerializer Serializer = new JsonMessageSerializer(null);
        static readonly ILog Logger = LogManager.GetLogger(typeof(SqlServerPollingDequeueStrategy));

        readonly ICircuitBreaker circuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("SqlTransportConnectivity", 
                            TimeSpan.FromMinutes(2),
                            ex => Configure.Instance.RaiseCriticalError("Repeated failures when communicating with SqlServer", ex),
                            TimeSpan.FromSeconds(10));

        Address addressToPoll;
        Action<TransportMessage, Exception> endProcessMessage;
        MTATaskScheduler scheduler;
        TransactionSettings settings;
        string sql;
        string tableName;
        CancellationTokenSource tokenSource;
        TransactionOptions transactionOptions;
        Func<TransportMessage, bool> tryProcessMessage;

    }
}