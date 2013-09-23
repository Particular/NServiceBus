namespace NServiceBus.SagaPersisters.NHibernate
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class LockModeAttribute : Attribute
    {
        public LockModes RequestedLockMode { get; private set; }

        public LockModeAttribute(LockModes lockModeToUse)
        {
            RequestedLockMode = lockModeToUse;
        }
    }

    public enum LockModes
    {
        None = 1,
        Read = 2,
        Upgrade = 3,
        UpgradeNoWait = 4,
        Write = 5,
        Force = 6
    }
}