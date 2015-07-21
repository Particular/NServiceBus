namespace NServiceBus.Core.Tests.Msmq
{
    using System.Messaging;

    public class MsmqTestsBase
    {
        protected static void DeleteQueue(string path)
        {
            if (!MessageQueue.Exists(path))
            {
                return;
            }
            MessageQueue.Delete(path);
        }

        protected static void CreateQueue(string path)
        {
            if (MessageQueue.Exists(path))
            {
                return;
            }
            using (MessageQueue.Create(path, true))
            {
            }
        }
    }
}