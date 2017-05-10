namespace NServiceBus
{
    using System.Threading.Tasks;

    interface ILearningTransportTransaction
    {
        string FileToProcess { get; }

        Task<bool> BeginTransaction(string incomingFilePath);

        Task Commit();

        void Rollback();

        void ClearPendingOutgoingOperations();

        Task Enlist(string messagePath, string messageContents);

        Task<bool> Complete();
    }
}