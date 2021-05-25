namespace NServiceBus
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    class DelayedMessagePoller
    {
        public DelayedMessagePoller(string basePath, string delayedDir)
        {
            this.basePath = basePath;

            delayedRootDirectory = delayedDir;
        }

        void MoveDelayedMessagesToMainDirectory()
        {
            foreach (var delayDir in new DirectoryInfo(delayedRootDirectory).EnumerateDirectories())
            {
                var timeToTrigger = DateTimeOffset.ParseExact(delayDir.Name, "yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal);

                if (DateTimeOffset.UtcNow >= timeToTrigger)
                {
                    foreach (var fileInfo in delayDir.EnumerateFiles())
                    {
                        File.Move(fileInfo.FullName, Path.Combine(basePath, fileInfo.Name));
                    }
                }

                //wait a bit more so we can safely delete the dir
                if (DateTimeOffset.UtcNow >= timeToTrigger.AddSeconds(10))
                {
                    Directory.Delete(delayDir.FullName);
                }
            }
        }

        public void Start()
        {
            polling = new CancellationTokenSource();

            _ = Task.Run(async () =>
              {
                  while (!polling.Token.IsCancellationRequested)
                  {
                      try
                      {
                          await Task.Delay(TimeSpan.FromSeconds(1), polling.Token).ConfigureAwait(false);
                      }
                      catch (Exception ex) when (ex.IsCausedBy(polling.Token))
                      {
                          // polling is being stopped
                          break;
                      }

                      try
                      {
                          MoveDelayedMessagesToMainDirectory();
                      }
                      catch (Exception ex)
                      {
                          Logger.Error("Unable to move expired messages to main input queue.", ex);
                      }
                  }
              });
        }

        public void Stop()
        {
            if (polling == null)
            {
                return;
            }

            polling.Cancel();
            polling.Dispose();
        }

        string delayedRootDirectory;
        CancellationTokenSource polling;
        string basePath;

        static ILog Logger = LogManager.GetLogger<DelayedMessagePoller>();
    }
}
