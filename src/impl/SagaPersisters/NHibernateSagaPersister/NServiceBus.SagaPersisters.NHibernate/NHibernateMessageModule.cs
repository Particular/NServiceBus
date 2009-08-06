using NHibernate;
using NHibernate.Context;

namespace NServiceBus.SagaPersisters.NHibernate
{
    /// <summary>
    /// Message module that manages NHibernate sessions.
    /// At the beginning of message handling, a session is opened,
    /// as the end, it is closed.
    /// </summary>
    public class NHibernateMessageModule : IMessageModule
    {
        void IMessageModule.HandleBeginMessage()
        {
            if (SessionFactory != null)
                CurrentSessionContext.Bind(SessionFactory.OpenSession());
        }

        void IMessageModule.HandleEndMessage()
        {
        }

        void IMessageModule.HandleError()
        {
        }

        /// <summary>
        /// Injected NHibernate session factory.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }
    }
}
