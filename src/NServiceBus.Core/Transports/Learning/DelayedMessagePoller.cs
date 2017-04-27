namespace NServiceBus
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using Logging;

    class DelayedMessagePoller
    {
        public DelayedMessagePoller(string basePath)
        {
            this.basePath = basePath;
            delayedMessagePoller = new Timer(MoveDelayedMessagesToMainDirectory);

            delayedRootDirectory = Path.Combine(basePath, ".delayed");
            Directory.CreateDirectory(delayedRootDirectory);
        }

        void MoveDelayedMessagesToMainDirectory(object state)
        {
            try
            {
                foreach (var delayDir in new DirectoryInfo(delayedRootDirectory).EnumerateDirectories())
                {
                    var timeToTrigger = DateTime.ParseExact(delayDir.Name, "yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);

                    if (DateTime.UtcNow >= timeToTrigger)
                        foreach (var fileInfo in delayDir.EnumerateFiles())
                            File.Move(fileInfo.FullName, Path.Combine(basePath, fileInfo.Name));

                    //wait a bit more so we can safely delete the dir
                    if (DateTime.UtcNow >= timeToTrigger.AddSeconds(10))
                        Directory.Delete(delayDir.FullName);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to trigger delayed messages", e);
            }
            finally
            {
                delayedMessagePoller.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
            }
        }

        public void Start()
        {
            delayedMessagePoller.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
        }

        string delayedRootDirectory;
        Timer delayedMessagePoller;

        static ILog Logger = LogManager.GetLogger<DelayedMessagePoller>();
        string basePath;
    }
}