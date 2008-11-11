using System;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Generated for sagas that don't come with their own finder.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EmptySagaFinder<T> : IFindSagas<T>.Using<IMessage> where T : ISagaEntity
    {
        public T FindBy(IMessage message)
        {
            return default(T);
        }
    }
}
