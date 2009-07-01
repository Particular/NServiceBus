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
        /// <summary>
        /// Opens a new NHibernate session using the injected session factory.
        /// </summary>
        public void HandleBeginMessage()
        {
            CurrentSessionContext.Bind(SessionFactory.OpenSession());
        }

        /// <summary>
        /// Closes the NHibernate session previously opened.
        /// </summary>
        public void HandleEndMessage()
        {
        }

        /// <summary>
        /// Injected NHibernate session factory.
        /// </summary>
        public ISessionFactory SessionFactory { get; set; }
    }
}
