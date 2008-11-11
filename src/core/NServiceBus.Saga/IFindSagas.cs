
namespace NServiceBus.Saga
{
    public interface IFinder { }

    public class IFindSagas<T> where T : ISagaEntity
    {
        public interface Using<M> : IFinder where M : IMessage
        {
            T FindBy(M message);
        }
    }
}
