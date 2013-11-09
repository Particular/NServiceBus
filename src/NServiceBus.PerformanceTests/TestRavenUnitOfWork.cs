namespace Runner
{
    using System;

    using NServiceBus.UnitOfWork;

    using Raven.Client;

    class TestRavenUnitOfWork : IManageUnitsOfWork
    {
        private readonly IDocumentSession session;

        public TestRavenUnitOfWork(IDocumentSession session)
        {
            this.session = session;
        }

        public void Begin()
        {
        }

        public void End(Exception ex = null)
        {
            if (ex == null)
            {
                session.SaveChanges();
            }
        }
    }
}