#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Transactions;

    [ObsoleteEx(Replacement = "Configure.With(b => b.Transactions(t => t.Enable())) or Configure.With(b => b.Transactions(t => t.Disable()))", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]      
    public static class TransactionalConfigManager
    {
        [ObsoleteEx(Replacement = "config.Transactions()", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure IsTransactional(this Configure config, bool value)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(b => b.Transactions(t => t.Disable()))", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure DontUseTransactions(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(b => b.Transactions(t => t.Advanced(a => a.IsolationLevel(IsolationLevel.Chaos))));", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]        
        public static Configure IsolationLevel(this Configure config, IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(Replacement = "Configure.With(b => b.Transactions(t => t.Advanced(a => a.DefaultTimeout(TimeSpan.FromMinutes(5)))));", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]                
        public static Configure TransactionTimeout(this Configure config, TimeSpan transactionTimeout)
        {
            throw new NotImplementedException();
        }
    }
}
