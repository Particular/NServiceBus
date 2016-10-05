namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    class RollingLogger
    {
        public RollingLogger(string targetDirectory, int numberOfArchiveFilesToKeep = 10, long maxFileSize = fileLimitInBytes)
        {
            this.targetDirectory = targetDirectory;
            this.numberOfArchiveFilesToKeep = numberOfArchiveFilesToKeep;
            this.maxFileSize = maxFileSize;
        }

        public void Write(string message)
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
                var errorMessage = $"NServiceBus.RollingLogger Could not write to log file '{currentfilePath}'. Exception: {exception}";
                Trace.WriteLine(errorMessage);
            }
        }

        protected virtual void AppendLine(string message)
        {
            var messageWithNewline = message + Environment.NewLine;
            File.AppendAllText(currentfilePath, messageWithNewline, Encoding.UTF8);
            currentFileSize += messageWithNewline.Length;
        }

        void SyncFileSystem()
        {
            if (!HasCurrentDateChanged() && !IsCurrentFileTooLarge())
            {
                return;
            }
            var today = GetDate();
            lastWriteDate = today;
            var nsbLogFiles = GetNsbLogFiles(targetDirectory).ToList();
            CalculateNewFileName(nsbLogFiles, today);
            PurgeOldFiles(nsbLogFiles);
        }

        bool HasCurrentDateChanged()
        {
            return GetDate() != lastWriteDate;
        }

        bool IsCurrentFileTooLarge()
        {
            return currentFileSize > maxFileSize;
        }

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

        IEnumerable<string> GetFilesToDelete(IEnumerable<LogFile> logFiles)
        {
            return logFiles
                .OrderByDescending(x => x.DatePart)
                .ThenByDescending(x => x.SequenceNumber)
                .Select(x => x.Path)
                .Skip(numberOfArchiveFilesToKeep);
        }

        internal static LogFile GetTodaysNewest(IEnumerable<LogFile> logFiles, DateTime today)
        {
            return logFiles.Where(x => x.DatePart == today)
                .OrderByDescending(x => x.SequenceNumber)
                .FirstOrDefault();
        }

        static IEnumerable<LogFile> GetNsbLogFiles(string targetDirectory)
        {
            foreach (var file in Directory.EnumerateFiles(targetDirectory, "nsb_log_*.txt"))
            {
                LogFile logFile;
                if (TryDeriveLogInformationFromPath(file, out logFile))
                {
                    yield return logFile;
                }
            }
        }

        static bool TryDeriveLogInformationFromPath(string file, out LogFile logFile)
        {
            logFile = null;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            var split = fileNameWithoutExtension.Split('_');
            if (split.Length != 4)
            {
                return false;
            }
            var datePart = split[2];

            DateTime dateTime;
            if (!TryParseDate(datePart, out dateTime))
            {
                return false;
            }

            var sequencePart = split[3];
            int sequenceNumber;
            if (!int.TryParse(sequencePart, out sequenceNumber))
            {
                return false;
            }

            logFile = new LogFile
            {
                DatePart = dateTime,
                SequenceNumber = sequenceNumber,
                Path = file
            };
            return true;
        }

        static bool TryParseDate(string datePart, out DateTime dateTime)
        {
            return DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }

        void CalculateNewFileName(List<LogFile> logFiles, DateTime today)
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
                var existingFileSize = new FileInfo(logFile.Path).Length;
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

            var fileName = $"nsb_log_{today.ToString("yyyy-MM-dd")}_{sequenceNumber}.txt";
            currentfilePath = Path.Combine(targetDirectory, fileName);
        }

        protected string currentfilePath;
        long currentFileSize;
        internal Func<DateTime> GetDate = () => DateTime.Now.Date;
        DateTime lastWriteDate;
        long maxFileSize;
        int numberOfArchiveFilesToKeep;
        string targetDirectory;
        const long fileLimitInBytes = 10L*1024*1024; //10MB

        internal class LogFile
        {
            public DateTime DatePart;
            public string Path;
            public int SequenceNumber;
        }
    }
}