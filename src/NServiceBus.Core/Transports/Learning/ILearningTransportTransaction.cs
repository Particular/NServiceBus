namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    interface ILearningTransportTransaction
    {
        string FileToProcess { get; }

        Task<bool> BeginTransaction(string incomingFilePath, CancellationToken cancellationToken = default);

        Task Commit(CancellationToken cancellationToken = default);

        void Rollback();

        void ClearPendingOutgoingOperations();

        Task Enlist(string messagePath, string messageContents, CancellationToken cancellationToken = default);

        bool Complete();
    }
}