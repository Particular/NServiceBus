#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

class RollingLogger(
    string targetDirectory,
    int numberOfArchiveFilesToKeep = 10,
    long maxFileSize = RollingLogger.fileLimitInBytes)
{
    public void WriteLine(string message)
    {
        SyncFileSystem();
        InnerWrite(message);
    }

    void InnerWrite(string message)
    {
        try
        {
            AppendLine(message);
        }
        catch (Exception exception)
        {
            var errorMessage = $"NServiceBus.RollingLogger Could not write to log file '{currentFilePath}'. Exception: {exception}";
            Trace.WriteLine(errorMessage);
        }
    }

    protected virtual void AppendLine(string message)
    {
        var messageWithNewline = message + Environment.NewLine;
        File.AppendAllText(currentFilePath, messageWithNewline, Encoding.UTF8);
        currentFileSize += messageWithNewline.Length;
    }

    void SyncFileSystem()
    {
        try
        {
            if (!HasCurrentDateChanged() && !IsCurrentFileTooLarge())
            {
                return;
            }
            var today = GetDate();
            var nsbLogFiles = GetNsbLogFiles(targetDirectory).ToList();
            lastWriteDate = today;
            CalculateNewFileName(nsbLogFiles, today);
            PurgeOldFiles(nsbLogFiles);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Tolerate environmental file system failures like the log directory or its files being removed
            // by overlapping processes, deployments, slot swaps or cleanups. Anything else is a bug and propagates.
            // lastWriteDate is only updated once the synchronization succeeded so a failed attempt is retried on the next write.
            var errorMessage = $"NServiceBus.RollingLogger Could not synchronize log files in directory '{targetDirectory}'. Exception: {exception}";
            Trace.WriteLine(errorMessage);
        }
    }

    bool HasCurrentDateChanged() => GetDate() != lastWriteDate;

    bool IsCurrentFileTooLarge() => currentFileSize > maxFileSize;

    void PurgeOldFiles(List<LogFile> logFiles)
    {
        foreach (var file in GetFilesToDelete(logFiles))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception)
            {
                var errorMessage = $"NServiceBus.RollingLogger Could not purge log file '{file}'. Exception: {exception}";
                InnerWrite(errorMessage);
            }
        }
    }

    IEnumerable<string> GetFilesToDelete(IEnumerable<LogFile> logFiles) =>
        logFiles
            .OrderByDescending(x => x.DatePart)
            .ThenByDescending(x => x.SequenceNumber)
            .Select(x => x.Path)
            .Skip(numberOfArchiveFilesToKeep);

    internal static LogFile? GetTodaysNewest(IEnumerable<LogFile> logFiles, DateTimeOffset today) =>
        logFiles.Where(x => x.DatePart.Date == today.Date)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault();

    static IEnumerable<LogFile> GetNsbLogFiles(string targetDirectory)
    {
        foreach (var file in Directory.EnumerateFiles(targetDirectory, "nsb_log_*.txt"))
        {
            if (TryDeriveLogInformationFromPath(file, out var logFile))
            {
                yield return logFile;
            }
        }
    }

    static bool TryDeriveLogInformationFromPath(string file, [NotNullWhen(true)] out LogFile? logFile)
    {
        logFile = null;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
        var split = fileNameWithoutExtension.Split('_');
        if (split.Length != 4)
        {
            return false;
        }
        var datePart = split[2];

        if (!TryParseDate(datePart, out var dateTime))
        {
            return false;
        }

        var sequencePart = split[3];
        if (!int.TryParse(sequencePart, out var sequenceNumber))
        {
            return false;
        }

        logFile = new LogFile(DatePart: dateTime, SequenceNumber: sequenceNumber, Path: file);
        return true;
    }

    static bool TryParseDate(string datePart, out DateTimeOffset dateTime) => DateTimeOffset.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);

    static long GetFileSizeOrZero(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The file can vanish between enumerating it and reading its metadata, e.g. removed by
            // overlapping processes, deployments or external cleanup. Treat it as empty so the
            // sequence number is reused and the file is recreated by the next append
            return 0;
        }
    }

    void CalculateNewFileName(List<LogFile> logFiles, DateTimeOffset today)
    {
        var logFile = GetTodaysNewest(logFiles, today);
        int sequenceNumber;
        if (logFile == null)
        {
            currentFileSize = 0;
            sequenceNumber = 0;
        }
        else
        {
            var existingFileSize = GetFileSizeOrZero(logFile.Path);
            if (existingFileSize > maxFileSize)
            {
                sequenceNumber = logFile.SequenceNumber + 1;
                currentFileSize = existingFileSize;
            }
            else
            {
                sequenceNumber = logFile.SequenceNumber;
                currentFileSize = 0;
            }
        }

        var fileName = $"nsb_log_{today:yyyy-MM-dd}_{sequenceNumber}.txt";
        currentFilePath = Path.Combine(targetDirectory, fileName);
    }

    protected string currentFilePath = string.Empty;
    long currentFileSize;
#pragma warning disable PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow - For rollover of log files, want to use local time
    internal Func<DateTimeOffset> GetDate = () => DateTimeOffset.Now.Date;
#pragma warning restore PS0023 // Use DateTime.UtcNow or DateTimeOffset.UtcNow
    DateTimeOffset lastWriteDate;
    const long fileLimitInBytes = 10L * 1024 * 1024; //10MB

    internal record LogFile(DateTimeOffset DatePart, string Path, int SequenceNumber);
}