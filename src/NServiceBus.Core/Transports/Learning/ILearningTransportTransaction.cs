namespace NServiceBus
{
    using System.Threading.Tasks;

    interface ILearningTransportTransaction
    {
        string FileToProcess { get; }

        void BeginTransaction(string incomingFilePath);

        Task Commit();

        void Rollback();

        void ClearPendingOutgoingOperations();

        Task Enlist(string messagePath, string messageContents);

        Task<bool> Complete();
    }
}