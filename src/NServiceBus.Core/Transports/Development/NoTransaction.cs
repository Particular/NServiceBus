namespace NServiceBus
{
    using System.Collections.Generic;
    using System.IO;

    class NoTransaction : IDevelopmentTransportTransaction
    {
        public string FileToProcess { get; private set; }

        public void BeginTransaction(string incomingFilePath)
        {
            FileToProcess = incomingFilePath;
        }

        public void Enlist(string messagePath, List<string> messageContents)
        {
            File.WriteAllLines(messagePath, messageContents);
        }

        public void Commit()
        {
            //no-op
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