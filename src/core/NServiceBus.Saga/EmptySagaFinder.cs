using System;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Generated for sagas that don't come with their own finder.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EmptySagaFinder<T> : IFindSagas<T> where T : ISagaEntity
    {
        #region IFindSagas<T> Members

        public T FindBy(IMessage message)
        {
            return default(T);
        }

        #endregion
    }
}
