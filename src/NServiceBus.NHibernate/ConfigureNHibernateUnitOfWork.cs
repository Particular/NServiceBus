namespace NServiceBus
{
    /// <summary>
    /// Contains extension methods to NServiceBus.Configure for the NHibernate unit of work manager.
    /// </summary>
    public static class ConfigureNHibernateUnitOfWork
    {
        /// <summary>
        /// Use the NHibernate backed unit of work implementation.
        /// </summary>
        [ObsoleteEx(Message = "You no longer need to call this method, a UOW is automatically created as part of the NServiceBus NHibernate Saga persister.", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        public static Configure NHibernateUnitOfWork(this Configure config)
        {
            return config;
        }
    }
}
