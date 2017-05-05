﻿namespace NServiceBus
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using Logging;

    class DelayedMessagePoller
    {
        public DelayedMessagePoller(string basePath)
        {
            this.basePath = basePath;
            delayedMessagePoller = new AsyncTimer();

            delayedRootDirectory = Path.Combine(basePath, ".delayed");
            Directory.CreateDirectory(delayedRootDirectory);
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
            delayedMessagePoller.Start(() =>
            {
                MoveDelayedMessagesToMainDirectory();
                return TaskEx.CompletedTask;
            }, TimeSpan.FromSeconds(1), ex => Logger.Error("Unable to move expired messages to main input queue.", ex));
        }

        public Task Stop()
        {
            return delayedMessagePoller.Stop();
        }

        string delayedRootDirectory;
        IAsyncTimer delayedMessagePoller;
        string basePath;

        static ILog Logger = LogManager.GetLogger<DelayedMessagePoller>();
    }
}