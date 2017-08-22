namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Logging;

    class NoTransaction : ILearningTransportTransaction
    {
        public NoTransaction(string basePath, string pendingDirName)
        {
            processingDirectory = Path.Combine(basePath, pendingDirName, Guid.NewGuid().ToString());
        }

        public string FileToProcess { get; private set; }

        public bool BeginTransaction(string incomingFilePath)
        {
            Directory.CreateDirectory(processingDirectory);
            FileToProcess = Path.Combine(processingDirectory, Path.GetFileName(incomingFilePath));

            try
            {
                File.Move(incomingFilePath, FileToProcess);
            }
            catch (IOException ex)
            {
                log.Debug($"Failed to move {incomingFilePath} to {FileToProcess}", ex);
                return false;
            }

            //seem like File.Move is not atomic at least within the same process so we need this extra check
            return File.Exists(FileToProcess);
        }

        public Task Enlist(string messagePath, string messageContents) => AsyncFile.WriteText(messagePath, messageContents);

        public Task Commit() => TaskEx.CompletedTask;

        public void Rollback() { }

        public void ClearPendingOutgoingOperations() { }

        public bool Complete()
        {
            Directory.Delete(processingDirectory, true);

            return true;
        }

        string processingDirectory;

        static ILog log = LogManager.GetLogger<NoTransaction>();
    }
}