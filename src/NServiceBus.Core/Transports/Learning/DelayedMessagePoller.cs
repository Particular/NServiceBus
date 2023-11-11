namespace NServiceBus;

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

        _ = PollForDelayedMessages(polling.Token);
    }

    async Task PollForDelayedMessages(CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
            {
                // private token, poller is being stopped, log the exception in case the stack trace is ever needed for debugging
                Logger.Debug("Operation canceled while stopping delayed message polling.", ex);
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

    readonly string delayedRootDirectory;
    CancellationTokenSource polling;
    readonly string basePath;

    static readonly ILog Logger = LogManager.GetLogger<DelayedMessagePoller>();
}