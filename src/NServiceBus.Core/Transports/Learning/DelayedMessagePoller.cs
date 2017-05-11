namespace NServiceBus
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Logging;

    class DelayedMessagePoller
    {
        public DelayedMessagePoller(string basePath, string delayedDir)
        {
            this.basePath = basePath;
            timer = new AsyncTimer();

            delayedRootDirectory = delayedDir;
        }

        void MoveDelayedMessagesToMainDirectory()
        {
            foreach (var delayDir in new DirectoryInfo(delayedRootDirectory).EnumerateDirectories())
            {
                var timeToTrigger = DateTime.ParseExact(delayDir.Name, "yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);

                if (DateTime.UtcNow >= timeToTrigger)
                {
                    foreach (var fileInfo in delayDir.EnumerateFiles())
                    {
                        File.Move(fileInfo.FullName, Path.Combine(basePath, fileInfo.Name));
                    }
                }

                //wait a bit more so we can safely delete the dir
                if (DateTime.UtcNow >= timeToTrigger.AddSeconds(10))
                {
                    Directory.Delete(delayDir.FullName);
                }
            }
        }

        public void Start()
        {
            timer.Start(() =>
            {
                MoveDelayedMessagesToMainDirectory();

                return TaskEx.CompletedTask;
            },
            TimeSpan.FromSeconds(1),
            ex => Logger.Error("Unable to move expired messages to main input queue.", ex));
        }

        public Task Stop()
        {
            return timer.Stop();
        }

        string delayedRootDirectory;
        IAsyncTimer timer;
        string basePath;

        static ILog Logger = LogManager.GetLogger<DelayedMessagePoller>();
    }
}
