namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    interface IDevelopmentTransportTransaction
    {
        string FileToProcess { get; }

        void BeginTransaction(string incomingFilePath);
        Task Commit();
        void Rollback();
        void ClearPendingOutgoingOperations();
        Task Enlist(string messagePath, List<string> messageContents);
        void Complete();
    }
}