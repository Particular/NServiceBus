namespace NServiceBus
{
    using System.Collections.Generic;

    class StartupDiagnosticEntries
    {
        public void Add(string sectionName, object section)
        {
            Entries.Add(new StartupDiagnosticEntry
            {
                Name = sectionName,
                Data = section
            });
        }

        internal List<StartupDiagnosticEntry> Entries = new List<StartupDiagnosticEntry>();

        public class StartupDiagnosticEntry
        {
            public string Name;
            public object Data;
        }
    }
}