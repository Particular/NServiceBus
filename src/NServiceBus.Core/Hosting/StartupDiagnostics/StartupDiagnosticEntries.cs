namespace NServiceBus
{
    using System.Collections.Generic;

    class StartupDiagnosticEntries
    {
        public void Add(string sectionName, object section)
        {
            entries.Add(new StartupDiagnosticEntry
            {
                Name = sectionName,
                Data = section
            });
        }

        public IEnumerable<StartupDiagnosticEntry> Entries => entries;

        List<StartupDiagnosticEntry> entries = new List<StartupDiagnosticEntry>();

        public class StartupDiagnosticEntry
        {
            public string Name;
            public object Data;
        }
    }
}