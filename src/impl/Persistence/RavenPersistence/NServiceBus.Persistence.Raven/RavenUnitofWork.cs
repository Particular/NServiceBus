using System;
using NServiceBus.UnitOfWork;

namespace NServiceBus.Persistence.Raven
{
    public class RavenUnitOfWork : IManageUnitsOfWork
    {
        readonly RavenSessionFactory sessionFactory;

        public RavenUnitOfWork(RavenSessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public void Begin()
        {
        }

        public void End(Exception ex)
        {
            if (ex != null) return;

            sessionFactory.SaveChanges();
        }
    }
}