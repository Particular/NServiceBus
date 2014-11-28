using System;
using System.Collections.Generic;

namespace NServiceBus.Persistence.FileSystem.Timeouts
{
    using System.IO;
    using NServiceBus.Timeout.Core;

    class FileSystemTimeoutPersister : IPersistTimeouts
    {
        private string filePath = @"z:\timeouts";

        public void Add(TimeoutData timeout)
        {
            // Each persister is assumed to generate its own ID for a TimeoutData object
            var timeoutId = Guid.NewGuid().ToString();
            File.AppendAllLines(filePath, new[] { timeout.ToTabbedString(timeoutId) });
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            var removed = false;
            timeoutData = null;
            var newLines = new List<string>();
            var lines = File.ReadAllLines(filePath);

            // Keep only lines registering timeouts other than the timeout marked for removal
            foreach (var line in lines)
            {
                var td = line.ToTimeoutData();
                if (timeoutId.Equals(td.Id))
                {
                    timeoutData = td;
                    removed = true;
                    continue;
                }
                newLines.Add(line);
            }

            File.WriteAllLines(filePath, newLines);
            return removed;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            var newLines = new List<string>();
            var lines = File.ReadAllLines(filePath);

            // Keep only lines registering timeouts with SagaId != sagaId
            foreach (var line in lines)
            {
                var td = line.ToTimeoutData();
                if (sagaId.Equals(td.SagaId))
                    continue;
                newLines.Add(line);
            }

            File.WriteAllLines(filePath, newLines);
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var ret = new List<Tuple<string, DateTime>>();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var td = line.ToTimeoutData();
                if (td.Time < startSlice)
                    continue;
                ret.Add(new Tuple<string, DateTime>(td.Id, td.Time));
            }

            nextTimeToRunQuery = DateTime.Now; // TODO

            return ret;
        }
    }
}
