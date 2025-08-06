#nullable enable
namespace NServiceBus;

using System.Collections.Generic;

/// <summary>
/// Holds diagnostics entries to be written at startup.
/// </summary>
public class StartupDiagnosticEntries
{
    /// <summary>
    /// Adds a new section to the diagnostics.
    /// </summary>
    public void Add(string sectionName, object section) =>
        entries.Add(new StartupDiagnosticEntry
        {
            Name = sectionName,
            Data = section
        });

    internal readonly List<StartupDiagnosticEntry> entries = [];

    /// <summary>
    /// A diagnostics section.
    /// </summary>
    public class StartupDiagnosticEntry
    {
        /// <summary>
        /// The section name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The actual diagnostics data.
        /// </summary>
        public required object Data { get; set; }
    }
}