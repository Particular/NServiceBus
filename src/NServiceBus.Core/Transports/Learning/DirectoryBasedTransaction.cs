namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    class DirectoryBasedTransaction : ILearningTransportTransaction
    {
        public DirectoryBasedTransaction(string basePath, string transactionId, bool immediateDispatch)
        {
            this.basePath = basePath;
            this.immediateDispatch = immediateDispatch;

            transactionDir = Path.Combine(basePath, PendingDirName, transactionId);
            commitDir = Path.Combine(basePath, CommittedDirName, transactionId);
        }

        public string FileToProcess { get; private set; }

        public void BeginTransaction(string incomingFilePath)
        {
            Directory.CreateDirectory(transactionDir);
            FileToProcess = Path.Combine(transactionDir, Path.GetFileName(incomingFilePath));
            File.Move(incomingFilePath, FileToProcess);
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
            outgoingFiles.Clear();
        }


        public Task Enlist(string messagePath, string messageContents)
        {
            if (immediateDispatch)
                return AsyncFile.WriteText(messagePath, messageContents);

            var inProgressFileName = Path.GetFileNameWithoutExtension(messagePath) + ".out";

            var txPath = Path.Combine(transactionDir, inProgressFileName);
            var committedPath = Path.Combine(commitDir, inProgressFileName);

            outgoingFiles.Add(new OutgoingFile(committedPath, messagePath));
            return AsyncFile.WriteText(txPath, messageContents);
        }


        public void Complete()
        {
            if (!committed)
                return;
            foreach (var outgoingFile in outgoingFiles)
                File.Move(outgoingFile.TxPath, outgoingFile.TargetPath);

            Directory.Delete(commitDir, true);
        }

        public static void RecoverPartiallyCompletedTransactions(string basePath)
        {
            var pendingRootDir = Path.Combine(basePath, PendingDirName);

            if (Directory.Exists(pendingRootDir))
            {
                foreach (var transactionDir in new DirectoryInfo(pendingRootDir).EnumerateDirectories())
                {
                    var transaction = new DirectoryBasedTransaction(basePath, transactionDir.Name, false);

                    transaction.RecoverPending();
                }
            }

            var comittedRootDir = Path.Combine(basePath, CommittedDirName);

            if (Directory.Exists(comittedRootDir))
            {
                foreach (var transactionDir in new DirectoryInfo(comittedRootDir).EnumerateDirectories())
                {
                    var transaction = new DirectoryBasedTransaction(basePath, transactionDir.Name, false);

                    transaction.RecoverCommitted();
                }
            }
        }

        void RecoverPending()
        {
            var pendingDir = new DirectoryInfo(transactionDir);

            //only need to move the incoming file
            foreach (var file in pendingDir.EnumerateFiles("*.txt"))
            {
                File.Move(file.FullName, Path.Combine(basePath, file.Name));
            }

            pendingDir.Delete(true);
        }

        void RecoverCommitted()
        {
            var committedDir = new DirectoryInfo(commitDir);

            //for now just rollback the completed ones as well. We could consider making this smarter in the future
            // but its good enough for now since duplicates is a possibility anyway
            foreach (var file in committedDir.EnumerateFiles("*.txt"))
            {
                File.Move(file.FullName, Path.Combine(basePath, file.Name));
            }

            committedDir.Delete(true);
        }

        string basePath;
        bool immediateDispatch;
        string commitDir;

        bool committed;

        List<OutgoingFile> outgoingFiles = new List<OutgoingFile>();
        string transactionDir;

        const string CommittedDirName = ".committed";
        const string PendingDirName = ".pending";

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