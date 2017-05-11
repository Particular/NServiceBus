namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    class NoTransaction : ILearningTransportTransaction
    {
        public NoTransaction(string basePath, string pendingDirName)
        {
            processingDirectory = Path.Combine(basePath, pendingDirName, Guid.NewGuid().ToString());
        }

        public string FileToProcess { get; private set; }

        public void BeginTransaction(string incomingFilePath)
        {
            Directory.CreateDirectory(processingDirectory);
            FileToProcess = Path.Combine(processingDirectory, Path.GetFileName(incomingFilePath));
            File.Move(incomingFilePath, FileToProcess);
        }

        public Task Enlist(string messagePath, string messageContents) => AsyncFile.WriteText(messagePath, messageContents);

        public Task Commit() => TaskEx.CompletedTask;

        public void Rollback() { }

        public void ClearPendingOutgoingOperations() { }

        public void Complete() => Directory.Delete(processingDirectory, true);

        string processingDirectory;
    }
}