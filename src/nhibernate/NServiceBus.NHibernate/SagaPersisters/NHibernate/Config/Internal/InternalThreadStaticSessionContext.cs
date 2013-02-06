namespace NServiceBus.SagaPersisters.NHibernate.Config.Internal
{
    using System;
    using global::NHibernate;
    using global::NHibernate.Context;
    using global::NHibernate.Engine;

    [Serializable]
    internal class InternalThreadStaticSessionContext : CurrentSessionContext
    {
        [ThreadStatic]
        private static ISession session;

        /// <summary>
        /// Gets or sets the currently bound session.
        /// </summary>
        protected override ISession Session
        {
            get
            {
                return session;
            }
            set
            {
                session = value;
            }
        }

        public InternalThreadStaticSessionContext(ISessionFactoryImplementor factory)
        {
        }
    }
}