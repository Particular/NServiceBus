#nullable enable
namespace NServiceBus.Hosting.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// Holds <see cref="AssemblyScanner.GetScannableAssemblies" /> results.
/// Contains list of errors and list of scannable assemblies.
/// </summary>
public class AssemblyScannerResults
{
    /// <summary>
    /// Constructor to initialize AssemblyScannerResults.
    /// </summary>
    public AssemblyScannerResults()
    {
        Assemblies = [];
        Types = [];
        SkippedFiles = [];
    }

    /// <summary>
    /// List of successfully found and loaded assemblies.
    /// </summary>
    public List<Assembly> Assemblies { get; private set; }

    /// <summary>
    /// List of files that were skipped while scanning because they were a) explicitly excluded
    /// by the user, b) not a .NET DLL, or c) not referencing NSB and thus not capable of implementing
    /// <see cref="IHandleMessages{T}" />.
    /// </summary>
    public List<SkippedFile> SkippedFiles { get; }

    /// <summary>
    /// True if errors where encountered during assembly scanning.
    /// </summary>
    public bool ErrorsThrownDuringScanning { get; internal set; }

    /// <summary>
    /// List of types.
    /// </summary>
    public List<Type> Types { get; private set; }

    internal void RemoveDuplicates()
    {
        var contextualReflectionContext = AssemblyLoadContext.CurrentContextualReflectionContext;
        var preferredAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in Assemblies)
        {
            var fullName = assembly.FullName;
            if (fullName is null)
            {
                continue;
            }

            if (!preferredAssemblies.TryGetValue(fullName, out var existing))
            {
                preferredAssemblies[fullName] = assembly;
                continue;
            }

            preferredAssemblies[fullName] = PreferAssembly(existing, assembly, contextualReflectionContext);
        }

        Assemblies = [.. preferredAssemblies.Values];

        var assemblySet = Assemblies.ToHashSet();
        Types = [.. Types.Where(t => assemblySet.Contains(t.Assembly)).Distinct()];
    }

    static Assembly PreferAssembly(Assembly left, Assembly right, AssemblyLoadContext? contextualReflectionContext)
    {
        if (contextualReflectionContext is not null)
        {
            var leftContext = AssemblyLoadContext.GetLoadContext(left);
            var rightContext = AssemblyLoadContext.GetLoadContext(right);

            if (leftContext == contextualReflectionContext && rightContext != contextualReflectionContext)
            {
                return left;
            }

            if (rightContext == contextualReflectionContext && leftContext != contextualReflectionContext)
            {
                return right;
            }
        }

        var leftIsDefault = AssemblyLoadContext.GetLoadContext(left) == AssemblyLoadContext.Default;
        var rightIsDefault = AssemblyLoadContext.GetLoadContext(right) == AssemblyLoadContext.Default;

        if (leftIsDefault != rightIsDefault)
        {
            return leftIsDefault ? left : right;
        }

        return left;
    }
}