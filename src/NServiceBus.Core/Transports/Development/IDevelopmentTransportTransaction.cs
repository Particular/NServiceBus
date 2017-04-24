namespace NServiceBus
{
    using System.Collections.Generic;

    interface IDevelopmentTransportTransaction
    {
        string FileToProcess { get; }

        void BeginTransaction(string incomingFilePath);
        void Commit();
        void Rollback();
        void ClearPendingOutgoingOperations();
        void Enlist(string messagePath, List<string> messageContents);
        void Complete();
    }
}