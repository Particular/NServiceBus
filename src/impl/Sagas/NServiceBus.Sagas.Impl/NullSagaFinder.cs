using NServiceBus.Saga;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Catch-all finder to return null - so that we can later check
    /// for whether a new saga should be created.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NullSagaFinder<T> : IFindSagas<T>.Using<IMessage> where T : ISagaEntity
    {
        /// <summary>
        /// Returns null.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public T FindBy(IMessage message)
        {
            return default(T);
        }
    }
}
