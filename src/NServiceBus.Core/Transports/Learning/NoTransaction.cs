namespace NServiceBus
{
    using System.IO;
    using System.Threading.Tasks;

    class NoTransaction : ILearningTransportTransaction
    {
        public string FileToProcess { get; private set; }

        public void BeginTransaction(string incomingFilePath)
        {
            FileToProcess = incomingFilePath;
        }

        public Task Enlist(string messagePath, string messageContents)
        {
            return AsyncFile.WriteText(messagePath, messageContents);
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