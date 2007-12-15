using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace Utils
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
        public void RunInTransaction(Callback callback)
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
            {
                callback();

                scope.Complete();
            }
        }
    }
}
