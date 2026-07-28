#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using Particular.Obsoletes;

/// <summary>
/// Holds diagnostics entries to be written at startup.
/// </summary>
public class StartupDiagnosticEntries
{
    /// <summary>
    /// Adds a new section to the diagnostics.
    /// </summary>
    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7883",
        ReplacementTypeOrMember = "Add<T>(string, T, JsonTypeInfo<T>)",
        Note = "The non-generic overload uses reflection-based serialization which is not AOT/trimming safe. Use the generic overload with a JsonTypeInfo<T> instead.")]
    public void Add(string sectionName, object section) =>
        entries.Add(new StartupDiagnosticEntry
        {
            Name = sectionName,
            Data = section
        });

    /// <summary>
    /// Adds a new section to the diagnostics with a strongly-typed value and its JSON type info for AOT-safe serialization.
    /// </summary>
    /// <remarks>
    /// Use this overload when the diagnostics value is cheap to compute.
    /// For expensive operations that should only run when diagnostics are actually written, use <see cref="AddFactory{T}"/> instead.
    /// </remarks>
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
    /// <remarks>
    /// Use this overload when the diagnostics value is expensive to compute and should only be evaluated
    /// when diagnostics are actually written. For cheap values, prefer <see cref="Add{T}"/> instead.
    /// </remarks>
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