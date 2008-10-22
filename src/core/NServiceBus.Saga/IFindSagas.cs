
namespace NServiceBus.Saga
{
    public interface IFinder { }

    public interface IFindSagas<T> : IFinder where T : ISagaEntity
    {
        T FindBy(IMessage message);
    }
}
