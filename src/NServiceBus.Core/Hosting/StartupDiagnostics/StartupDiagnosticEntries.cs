#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

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

    /// <summary>
    /// Adds a new section to the diagnostics with a strongly-typed value and its JSON type info for AOT-safe serialization.
    /// </summary>
    public void Add<T>(string sectionName, T section, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(section);
        entries.Add(new StartupDiagnosticEntry
        {
            Name = sectionName,
            Data = section,
            JsonTypeInfo = typeInfo
        });
    }

    /// <summary>
    /// Adds a new section to the diagnostics with a factory that is evaluated lazily and its JSON type info for AOT-safe serialization.
    /// </summary>
    public void AddFactory<T>(string sectionName, Func<T> sectionFactory, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(sectionFactory);
        ArgumentNullException.ThrowIfNull(typeInfo);
        entries.Add(new StartupDiagnosticEntry
        {
            Name = sectionName,
            Data = null!,
            JsonTypeInfo = typeInfo,
            Factory = () => sectionFactory()!
        });
    }

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

        internal JsonTypeInfo? JsonTypeInfo { get; set; }

        internal Func<object>? Factory { get; set; }
    }
}