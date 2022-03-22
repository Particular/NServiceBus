namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUnitOfWorkMessageSessionFactory
    {
        Task<IUnitOfWorkMessageSession> OpenSession(string? sessionId = default, CancellationToken cancellationToken = default);
    }
}