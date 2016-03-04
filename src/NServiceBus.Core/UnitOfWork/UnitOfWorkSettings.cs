namespace NServiceBus
{
    using System;
    using System.Transactions;
    using Features;

    /// <summary>
    /// Configuration class for Unit Of Work settings.
    /// </summary>
    public class UnitOfWorkSettings
    {
        internal UnitOfWorkSettings(EndpointConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Wraps <see cref="IHandleMessages{T}">handlers</see> in a <see cref="TransactionScope" /> to make sure all storage
        /// operations becomes part of the same unit of work.
        /// </summary>
        public UnitOfWorkSettings WrapHandlersInATransactionScope(TimeSpan? timeout = null, IsolationLevel? isolationLevel = null)
        {
            config.EnableFeature<TransactionScopeUnitOfWork>();
            config.Settings.Set<TransactionScopeUnitOfWork.Settings>(new TransactionScopeUnitOfWork.Settings(timeout, isolationLevel));
            return this;
        }

        EndpointConfiguration config;
    }
}