namespace NServiceBus;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataBus;
using Logging;

class FileShareDataBusImplementation : IDataBus
{
    // to account for mixed platforms ie windows -> linux or linux -> windows
    internal class PathNormalizer
    {
        //    Example keys
        //    string key1 = "foldername/filename";
        //    string key2 = "foldername\\filename";

        //    Normalize the keys
        //    string normalizedKey1 = NormalizePath(key1);
        //    string normalizedKey2 = NormalizePath(key2);

        //    Output the normalized keys
        //    Console.WriteLine(normalizedKey1); // Output will be "foldername\filename" on Windows, "foldername/filename" on Unix-based systems
        //    Console.WriteLine(normalizedKey2); // Output will be "foldername\filename" on Windows, "foldername/filename" on Unix-based systems 
        internal static string NormalizePath(string key)
        {
            // Determine the directory separator for the current platform
            char separator = Path.DirectorySeparatorChar;
            // Replace any forward slashes (common in URIs) and backward slashes with the platform-specific separator
            string normalizedPath = key.Replace('/', separator).Replace('\\', separator);

            return normalizedPath;
        }
    }


    public FileShareDataBusImplementation(string basePath)
    {
        this.basePath = basePath;
    }

    public TimeSpan MaxMessageTimeToLive { get; set; }

    public Task<Stream> Get(string key, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(basePath, PathNormalizer.NormalizePath(key));

        logger.DebugFormat("Opening stream from '{0}'.", filePath);

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        return Task.FromResult((Stream)fileStream);
    }

    public async Task<string> Put(Stream stream, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default)
    {
        var key = GenerateKey(timeToBeReceived);

        var filePath = Path.Combine(basePath, key);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous))
        {
            const int bufferSize = 32 * 1024;
            await stream.CopyToAsync(output, bufferSize, cancellationToken).ConfigureAwait(false);
        }

        logger.DebugFormat("Saved stream to '{0}'.", filePath);

        return key;
    }

    public Task Start(CancellationToken cancellationToken = default)
    {
        logger.Info("File share data bus started. Location: " + basePath);
        //TODO: Implement a clean up thread
        return Task.CompletedTask;
    }

    string GenerateKey(TimeSpan timeToBeReceived)
    {
        if (timeToBeReceived > MaxMessageTimeToLive)
        {
            timeToBeReceived = MaxMessageTimeToLive;
        }

        var keepMessageUntil = DateTimeOffset.MaxValue;

        if (timeToBeReceived < TimeSpan.MaxValue)
        {
            keepMessageUntil = DateTimeOffset.UtcNow + timeToBeReceived;
        }

        return Path.Combine(keepMessageUntil.ToString("yyyy-MM-dd_HH"), Guid.NewGuid().ToString());
    }

    readonly string basePath;
    static readonly ILog logger = LogManager.GetLogger<FileShareDataBusImplementation>();
}
