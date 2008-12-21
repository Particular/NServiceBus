using System;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Returns null when asked to find a saga using a message.
    /// Generated for sagas that don't come with their own finder.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EmptySagaFinder<T> : IFindSagas<T>.Using<IMessage> where T : class, ISagaEntity
    {
        /// <summary>
        /// Returns null for all messages.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>null.</returns>
        public T FindBy(IMessage message)
        {
            return null;
        }
    }
}
