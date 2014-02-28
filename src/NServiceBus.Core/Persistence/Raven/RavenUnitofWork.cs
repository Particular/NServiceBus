namespace NServiceBus.Persistence.Raven
{
    using System;
    using UnitOfWork;

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
            try
            {
                if (ex == null)
                {
                    sessionFactory.SaveChanges();
                }
            }
            finally
            {
                sessionFactory.ReleaseSession();
            }
        }
    }
}