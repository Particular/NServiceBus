namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    class DirectoryBasedTransaction : ILearningTransportTransaction
    {
        public DirectoryBasedTransaction(string basePath, bool immediateDispatch)
        {
            this.basePath = basePath;
            this.immediateDispatch = immediateDispatch;
            var transactionId = Guid.NewGuid().ToString();

            transactionDir = Path.Combine(basePath, ".pending", transactionId);

            commitDir = Path.Combine(basePath, ".committed", transactionId);
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
            {
                return AsyncFile.WriteText(messagePath, messageContents);
            }
            var txPath = Path.Combine(transactionDir, Path.GetFileName(messagePath));
            var committedPath = Path.Combine(commitDir, Path.GetFileName(messagePath));

            outgoingFiles.Add(new OutgoingFile(committedPath, messagePath));
            return AsyncFile.WriteText(txPath, messageContents);
        }


        public void Complete()
        {
            if (!committed)
            {
                return;
            }
            foreach (var outgoingFile in outgoingFiles)
            {
                File.Move(outgoingFile.TxPath, outgoingFile.TargetPath);
            }

            Directory.Delete(commitDir, true);
        }

        string basePath;
        bool immediateDispatch;
        string commitDir;

        bool committed;

        List<OutgoingFile> outgoingFiles = new List<OutgoingFile>();
        string transactionDir;

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