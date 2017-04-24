namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class DirectoryBasedTransaction : IDevelopmentTransportTransaction
    {
        public DirectoryBasedTransaction(string basePath)
        {
            this.basePath = basePath;
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

        public async Task Commit()
        {
            var dispatchFile = Path.Combine(transactionDir, "dispatch.txt");
            await AsyncFile.WriteLines(dispatchFile, outgoingFiles.Select(file => $"{file.TxPath}=>{file.TargetPath}").ToArray())
                .ConfigureAwait(false);

            Directory.Move(transactionDir, commitDir);
            committed = true;
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


        public async Task Enlist(string messagePath, string messageContents)
        {
            var txPath = Path.Combine(transactionDir, Path.GetFileName(messagePath));
            var committedPath = Path.Combine(commitDir, Path.GetFileName(messagePath));

            await AsyncFile.WriteText(txPath, messageContents)
                .ConfigureAwait(false);
            outgoingFiles.Add(new OutgoingFile(committedPath, messagePath));
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