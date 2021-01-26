namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    interface ILearningTransportTransaction
    {
        string FileToProcess { get; }

        Task<bool> BeginTransaction(string incomingFilePath, CancellationToken cancellationToken);

        Task Commit(CancellationToken cancellationToken);

        void Rollback();

        void ClearPendingOutgoingOperations();

        Task Enlist(string messagePath, string messageContents, CancellationToken cancellationToken);

        bool Complete();
    }
}