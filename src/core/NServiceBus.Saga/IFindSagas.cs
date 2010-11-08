
namespace NServiceBus.Saga
{
    /// <summary>
    /// Marker interface for <see cref="IFindSagas{T}.Using{M}"/>
    /// </summary>
    public interface IFinder { }

    /// <summary>
    /// Interface indicating that implementers can find sagas of the given type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IFindSagas<T> where T : ISagaEntity
    {
        /// <summary>
        /// Narrower interface indicating that implementers can find sagas
        /// of type T using messages of type M.
        /// </summary>
        /// <typeparam name="M"></typeparam>
        public interface Using<M> : IFinder where M : IMessage
        {
            /// <summary>
            /// Finds a saga entity of the type T using a message of type M.
            /// </summary>
            /// <param name="message"></param>
            /// <returns></returns>
            T FindBy(M message);
        }
    }
}
