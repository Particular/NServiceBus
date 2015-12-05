namespace NServiceBus.Sagas
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Persistence;

    /// <summary>
    /// Interface indicating that implementers can find sagas of the given type.
    /// </summary>
    public abstract class IFindSagas<T> where T : IContainSagaData
    {
        /// <summary>
        /// Narrower interface indicating that implementers can find sagas
        /// of type T using messages of type M.
        /// </summary>
        public interface Using<M> : IFinder
        {
            /// <summary>
            /// Finds a saga entity of the type T using a message of type M.
            /// </summary>
            Task<T> FindBy(M message, ISynchronizedStorageSession storageSession, ReadOnlyContextBag context);
        }
    }
}
