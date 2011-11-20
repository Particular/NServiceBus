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
            Console.WriteLine("UoW: Begin");
        }

        public void End(Exception ex)
        {
            if (ex == null)
                Console.WriteLine("UoW: Commit");
            else
                Console.WriteLine("UoW: Rollback, reason: " + ex);
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