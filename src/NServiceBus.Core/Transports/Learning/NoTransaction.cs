namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    class NoTransaction : ILearningTransportTransaction
    {
        public string FileToProcess { get; private set; }
        string processingDirectory;

        public NoTransaction(string basePath)
        {
            processingDirectory = Path.Combine(basePath, ".notxprocessing", Guid.NewGuid().ToString());
        }

        public void BeginTransaction(string incomingFilePath)
        {
            Directory.CreateDirectory(processingDirectory);
            FileToProcess = Path.Combine(processingDirectory, Path.GetFileName(incomingFilePath));
            File.Move(incomingFilePath, FileToProcess);
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
            Directory.Delete(processingDirectory, true);
        }
    }
}