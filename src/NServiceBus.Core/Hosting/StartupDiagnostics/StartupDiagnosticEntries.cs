namespace NServiceBus
{
    using System.Collections.Generic;

    /// <summary>
    /// Holds diagnostics entries to be written at startup.
    /// </summary>
    public class StartupDiagnosticEntries
    {
        /// <summary>
        /// Adds a new section to the diagnostics.
        /// </summary>
        public void Add(string sectionName, object section)
        {
            entries.Add(new StartupDiagnosticEntry
            {
                Name = sectionName,
                Data = section
            });
        }

        internal List<StartupDiagnosticEntry> entries = new List<StartupDiagnosticEntry>();

        /// <summary>
        /// A diagnostics section.
        /// </summary>
        public class StartupDiagnosticEntry
        {
            /// <summary>
            /// The section name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The actual diagnostics data.
            /// </summary>
            public object Data { get; set; }
        }
    }
}