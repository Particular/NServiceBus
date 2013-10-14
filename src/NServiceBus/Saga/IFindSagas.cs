
namespace NServiceBus.Saga
{
    /// <summary>
    /// Marker interface for <see cref="IFindSagas{T}.Using{M}"/>
    /// </summary>
    public interface IFinder { }

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
            T FindBy(M message);
        }
    }
}
