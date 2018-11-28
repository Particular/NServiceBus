namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using Logging;

    class DirectoryBasedTransaction : ILearningTransportTransaction
    {
        public DirectoryBasedTransaction(string basePath, string pendingDirName, string committedDirName, string transactionId)
        {
            this.basePath = basePath;

            transactionDir = Path.Combine(basePath, pendingDirName, transactionId);
            commitDir = Path.Combine(basePath, committedDirName, transactionId);
        }

        public string FileToProcess { get; private set; }

        public Task<bool> BeginTransaction(string incomingFilePath)
        {
            Directory.CreateDirectory(transactionDir);
            FileToProcess = Path.Combine(transactionDir, Path.GetFileName(incomingFilePath));

            return AsyncFile.Move(incomingFilePath, FileToProcess);
        }

        public Task Commit()
        {
            Directory.Move(transactionDir, commitDir);
            committed = true;

            return TaskEx.CompletedTask;
        }

        public void Rollback()
        {
            //rollback by moving the file back to the main dir
            File.Move(FileToProcess, Path.Combine(basePath, Path.GetFileName(FileToProcess)));
            Directory.Delete(transactionDir, true);
        }

        public void ClearPendingOutgoingOperations()
        {
            while (outgoingFiles.TryDequeue(out _)) { }
        }

        public Task Enlist(string messagePath, string messageContents)
        {
            var inProgressFileName = Path.GetFileNameWithoutExtension(messagePath) + ".out";

            var txPath = Path.Combine(transactionDir, inProgressFileName);
            var committedPath = Path.Combine(commitDir, inProgressFileName);

            outgoingFiles.Enqueue(new OutgoingFile(committedPath, messagePath));

            return AsyncFile.WriteText(txPath, messageContents);
        }

        public bool Complete()
        {
            if (!committed)
            {
                return false;
            }

            while (outgoingFiles.TryDequeue(out var outgoingFile))
            {
                File.Move(outgoingFile.TxPath, outgoingFile.TargetPath);
            }

            Directory.Delete(commitDir, true);

            return true;
        }

        public static void RecoverPartiallyCompletedTransactions(string basePath, string pendingDirName, string committedDirName)
        {
            var pendingRootDir = Path.Combine(basePath, pendingDirName);

            if (Directory.Exists(pendingRootDir))
            {
                foreach (var transactionDir in new DirectoryInfo(pendingRootDir).EnumerateDirectories())
                {
                    new DirectoryBasedTransaction(basePath, pendingDirName, committedDirName, transactionDir.Name)
                        .RecoverPending();
                }
            }

            var committedRootDir = Path.Combine(basePath, committedDirName);

            if (Directory.Exists(committedRootDir))
            {
                foreach (var transactionDir in new DirectoryInfo(committedRootDir).EnumerateDirectories())
                {
                    new DirectoryBasedTransaction(basePath, pendingDirName, committedDirName, transactionDir.Name)
                        .RecoverCommitted();
                }
            }
        }

        void RecoverPending()
        {
            var pendingDir = new DirectoryInfo(transactionDir);

            try
            {
                //only need to move the incoming file
                foreach (var file in pendingDir.EnumerateFiles(TxtFileExtension))
                {
                    var destFileName = Path.Combine(basePath, file.Name);
                    try
                    {
                        File.Move(file.FullName, destFileName);
                    }
                    catch (Exception e)
                    {
                        log.Debug($"Unable to move pending transaction from '{file.FullName}' to '{destFileName}'. Pending transaction is assumed to be recovered by a competing consumer.", e);
                    }
                }


                pendingDir.Delete(true);
            }
            catch (Exception e)
            {
                log.Debug($"Unable to recover pedning transaction '{pendingDir.FullName}'.", e);
            }
        }

        void RecoverCommitted()
        {
            var committedDir = new DirectoryInfo(commitDir);

            //for now just rollback the completed ones as well. We could consider making this smarter in the future
            // but its good enough for now since duplicates is a possibility anyway
            foreach (var file in committedDir.EnumerateFiles(TxtFileExtension))
            {
                var destFileName = Path.Combine(basePath, file.Name);
                try
                {
                    File.Move(file.FullName, destFileName);
                }
                catch (Exception e)
                {
                    log.Debug($"Unable to move committed transaction from '{file.FullName}' to '{destFileName}'. Committed transaction is assumed to be recovered by a competing consumer.", e);
                }
            }

            try
            {
                committedDir.Delete(true);
            }
            catch (Exception e)
            {
                log.Debug($"Unable to delete committed transaction directory '{committedDir.FullName}'.", e);
            }
        }

        string basePath;
        string commitDir;

        bool committed;

        ConcurrentQueue<OutgoingFile> outgoingFiles = new ConcurrentQueue<OutgoingFile>();
        string transactionDir;

        const string TxtFileExtension = "*.txt";

        static ILog log = LogManager.GetLogger<DirectoryBasedTransaction>();

        class OutgoingFile
        {
            public OutgoingFile(string txPath, string targetPath)
            {
                TxPath = txPath;
                TargetPath = targetPath;
            }

            public string TxPath { get; }
            public string TargetPath { get; }
        }
    }
}
