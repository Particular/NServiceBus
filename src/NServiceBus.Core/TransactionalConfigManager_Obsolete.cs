#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;
    using System.Transactions;

    [ObsoleteEx(
        Message = "Use `configuration.Transactions().Enable()` or `configuration.Transactions().Disable()`, where `configuration` is an instance of type `BusConfiguration`.", 
        TreatAsErrorFromVersion = "5.0",
        RemoveInVersion = "6.0")]      
    public static class TransactionalConfigManager
    {
        [ObsoleteEx(
            Message = "Use `configuration.Transactions()`, where `configuration` is an instance of type `BusConfiguration`.",
            TreatAsErrorFromVersion = "5.0", 
            RemoveInVersion = "6.0")]
        public static Configure IsTransactional(this Configure config, bool value)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.Transactions().Disable()`, where `configuration` is an instance of type `BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]
        public static Configure DontUseTransactions(this Configure config)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.Transactions().IsolationLevel(IsolationLevel.Chaos)`, where `configuration` is an instance of type `BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0",
            RemoveInVersion = "6.0")]        
        public static Configure IsolationLevel(this Configure config, IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(
            Message = "Use `configuration.Transactions().DefaultTimeout(TimeSpan.FromMinutes(5))`, where `configuration` is an instance of type `BusConfiguration`.", 
            TreatAsErrorFromVersion = "5.0", 
            RemoveInVersion = "6.0")]                
        public static Configure TransactionTimeout(this Configure config, TimeSpan transactionTimeout)
        {
            throw new NotImplementedException();
        }
    }
}
