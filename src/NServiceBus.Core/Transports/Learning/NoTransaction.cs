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

        public Task<bool> BeginTransaction(string incomingFilePath)
        {
            Directory.CreateDirectory(processingDirectory);
            FileToProcess = Path.Combine(processingDirectory, Path.GetFileName(incomingFilePath));

            return AsyncFile.Move(incomingFilePath, FileToProcess);
        }

        public Task Enlist(string messagePath, string messageContents) => AsyncFile.WriteText(messagePath, messageContents);

        public Task Commit() => TaskEx.CompletedTask;

        public void Rollback() { }

        public void ClearPendingOutgoingOperations() { }

        public Task<bool> Complete()
        {
            Directory.Delete(processingDirectory, true);

            return Task.FromResult(true);
        }

        string processingDirectory;
    }
}