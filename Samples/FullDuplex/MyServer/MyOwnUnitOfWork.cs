namespace MyServer
{
    using System;
    using NServiceBus;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.UnitOfWork;

    public class MyOwnUnitOfWork : IManageUnitsOfWork
    {
        public void Begin()
        {
            LogMessage("Begin");
        }

        public void End(Exception ex)
        {
            if (ex == null)
                LogMessage("Commit");
            else
                LogMessage("Rollback, reason: " + ex);
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("UoW({0}) - {1}",GetHashCode(), message));
        }
    }

    public class UoWIntitializer : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<MyOwnUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork);
        }
    }
}