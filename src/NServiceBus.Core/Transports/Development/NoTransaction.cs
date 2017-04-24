namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    class NoTransaction : IDevelopmentTransportTransaction
    {
        public string FileToProcess { get; private set; }

        public void BeginTransaction(string incomingFilePath)
        {
            FileToProcess = incomingFilePath;
        }

        public Task Enlist(string messagePath, List<string> messageContents)
        {
            return AsyncFile.WriteLines(messagePath, messageContents);
        }

        public Task Commit()
        {
            //no-op
            return TaskEx.CompletedTask;
        }

        public void Rollback()
        {
            //no-op
        }

        public void ClearPendingOutgoingOperations()
        {
            //no-op
        }


        public void Complete()
        {
            File.Delete(FileToProcess);
        }
    }
}