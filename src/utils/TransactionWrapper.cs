using System;
using System.Diagnostics;
using System.Transactions;

namespace NServiceBus.Utils
{
	/// <summary>
	/// Provides functionality for executing a callback in a transaction.
	/// </summary>
    public class TransactionWrapper
    {
		/// <summary>
		/// Executes the provided delegate method in a transaction.
		/// </summary>
		/// <param name="callback">The delegate method to call.</param>
        [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
        public void RunInTransaction(Callback callback)
        {
            RunInTransaction(callback, IsolationLevel.Serializable, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Executes the provided delegate method in a transaction.
        /// </summary>
        /// <param name="callback">The delegate method to call.</param>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <param name="transactionTimeout">The timeout period of the transaction.</param>
        [DebuggerNonUserCode] // so that exceptions don't interfere with debugging.
        public void RunInTransaction(Callback callback, IsolationLevel isolationLevel, TimeSpan transactionTimeout)
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = isolationLevel, Timeout = transactionTimeout }))
            {
                callback();

                scope.Complete();
            }
        }
    }
}
