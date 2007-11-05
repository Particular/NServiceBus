using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace Utils
{
    public class TransactionWrapper
    {
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
