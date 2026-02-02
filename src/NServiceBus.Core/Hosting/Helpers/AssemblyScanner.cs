#nullable enable
namespace NServiceBus.Hosting.Helpers;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using Logging;

/// <summary>
/// Helpers for assembly scanning operations.
/// </summary>
public class AssemblyScanner
{
    /// <summary>
    /// Creates a new scanner that will scan the base directory of the current <see cref="AppDomain" />.
    /// </summary>
    public AssemblyScanner()
        : this(AppDomain.CurrentDomain.BaseDirectory)
    {
    }

    /// <summary>
    /// Creates a scanner for the given directory.
    /// </summary>
    public AssemblyScanner(string baseDirectoryToScan) => this.baseDirectoryToScan = baseDirectoryToScan;

    internal AssemblyScanner(Assembly assemblyToScan) => this.assemblyToScan = assemblyToScan;

    /// <summary>
    /// Determines if the scanner should throw exceptions or not.
    /// </summary>
    public bool ThrowExceptions { get; set; } = true;

    /// <summary>
    /// Determines if the scanner should scan assemblies loaded in the <see cref="AppDomain.CurrentDomain" />.
    /// </summary>
    public bool ScanAppDomainAssemblies { get; set; } = true;

    /// <summary>
    /// Determines if the scanner should scan assemblies from the file system.
    /// </summary>
    public bool ScanFileSystemAssemblies { get; set; } = true;

    internal IReadOnlyCollection<string> AssembliesToSkip
    {
        set => assembliesToSkip = new HashSet<string>(value.Select(RemoveExtension), StringComparer.OrdinalIgnoreCase);
    }

    static string RemoveExtension(string assemblyName) =>
        assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || assemblyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(assemblyName)
            : assemblyName;

    internal IReadOnlyCollection<Type> TypesToSkip
    {
        set => typesToSkip = [.. value];
    }

    internal string? AdditionalAssemblyScanningPath { get; set; }

    /// <summary>
    /// Traverses the specified base directory including all sub-directories, generating a list of assemblies that should be
    /// scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
    /// </summary>
    public AssemblyScannerResults GetScannableAssemblies()
    {
        var results = new AssemblyScannerResults();
        var processed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { GetType().Assembly.FullName!, true },
            { typeof(ICommand).Assembly.FullName!, true },
        };

        var assemblyLoadContext = GetAssemblyLoadContext();
        if (assemblyToScan is not null)
        {
            if (ScanAssembly(assemblyToScan, processed, assemblyLoadContext))
            {
                AddTypesToResult(assemblyToScan, results);
            }

            return results;
        }

        if (ScanAppDomainAssemblies)
        {
            var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in appDomainAssemblies)
            {
                if (ScanAssembly(assembly, processed, assemblyLoadContext))
                {
                    AddTypesToResult(assembly, results);
                }
            }
        }

        if (ScanFileSystemAssemblies)
        {
            var assemblies = new List<Assembly>();

            if (baseDirectoryToScan is not null)
            {
                ScanAssembliesInDirectory(baseDirectoryToScan, assemblies, results, assemblyLoadContext);
            }

            if (!string.IsNullOrWhiteSpace(AdditionalAssemblyScanningPath))
            {
                ScanAssembliesInDirectory(AdditionalAssemblyScanningPath, assemblies, results, assemblyLoadContext);
            }

            var platformAssembliesString = (string?)AppDomain.CurrentDomain.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

            if (!string.IsNullOrEmpty(platformAssembliesString))
            {
                var platformAssemblies = platformAssembliesString.Split(Path.PathSeparator);

                foreach (var platformAssembly in platformAssemblies)
                {
                    if (TryLoadScannableAssembly(platformAssembly, results, assemblyLoadContext, out var assembly))
                    {
                        assemblies.Add(assembly);
                    }
                }
            }

            foreach (var assembly in assemblies)
            {
                if (ScanAssembly(assembly, processed, assemblyLoadContext))
                {
                    AddTypesToResult(assembly, results);
                }
            }
        }

        results.RemoveDuplicates();
        return results;
    }

    void ScanAssembliesInDirectory(string directoryToScan, List<Assembly> assemblies, AssemblyScannerResults results, AssemblyLoadContext assemblyLoadContext)
    {
        foreach (var assemblyFile in ScanDirectoryForAssemblyFiles(directoryToScan, ScanNestedDirectories))
        {
            if (TryLoadScannableAssembly(assemblyFile.FullName, results, assemblyLoadContext, out var assembly))
            {
                assemblies.Add(assembly);
            }
        }
    }

    bool TryLoadScannableAssembly(string assemblyPath, AssemblyScannerResults results, AssemblyLoadContext assemblyLoadContext, [NotNullWhen(true)] out Assembly? assembly)
    {
        assembly = null;

        if (IsExcluded(Path.GetFileNameWithoutExtension(assemblyPath)))
        {
            var skippedFile = new SkippedFile(assemblyPath, "File was explicitly excluded from scanning.");
            results.SkippedFiles.Add(skippedFile);

            return false;
        }

        AssemblyValidator.ValidateAssemblyFile(assemblyPath, out var shouldLoad, out var reason);

        if (!shouldLoad)
        {
            var skippedFile = new SkippedFile(assemblyPath, reason);
            results.SkippedFiles.Add(skippedFile);

            return false;
        }

        try
        {
            assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
            return true;
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
        {
            results.ErrorsThrownDuringScanning = true;

            if (ThrowExceptions)
            {
                var errorMessage = $"Could not load '{assemblyPath}'. Consider excluding that assembly from the scanning.";
                throw new Exception(errorMessage, ex);
            }

            var skippedFile = new SkippedFile(assemblyPath, ex.Message);
            results.SkippedFiles.Add(skippedFile);

            return false;
        }
    }

    // AssemblyScanner can run under hosts that use custom AssemblyLoadContexts (e.g. test runners, plugins).
    // When such a host sets a contextual reflection scope, we must load scanned assemblies into that context
    // to avoid loading the same assembly again into Default ALC (type identity issues, module initializers running twice).
    // If no contextual scope is set, preserve historical behavior (load into the ALC that loaded NServiceBus.Core),
    // and fall back to Default to keep scanning deterministic if the ALC cannot be determined.
    static AssemblyLoadContext GetAssemblyLoadContext() =>
        AssemblyLoadContext.CurrentContextualReflectionContext
        ?? AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())
        ?? AssemblyLoadContext.Default;

    bool ScanAssembly(Assembly assembly, Dictionary<string, bool> processed, AssemblyLoadContext fallbackAssemblyLoadContext)
    {
        if (assembly.FullName is null)
        {
            return false;
        }

        if (processed.TryGetValue(assembly.FullName, out var value))
        {
            return value;
        }

        processed[assembly.FullName] = false;

        if (ShouldScanDependencies(assembly))
        {
            // AppDomain assemblies can originate from different ALCs (test runners, plugin hosts).
            // When scanning dependencies for an already-loaded assembly, prefer resolving referenced assemblies
            // using the same ALC that loaded that assembly to avoid pulling them into a different ALC (e.g. Default).
            var assemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly) ?? fallbackAssemblyLoadContext;

            foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                var referencedAssembly = GetReferencedAssembly(referencedAssemblyName, assemblyLoadContext);
                if (referencedAssembly is not null)
                {
                    var referencesCore = ScanAssembly(referencedAssembly, processed, fallbackAssemblyLoadContext);
                    if (referencesCore)
                    {
                        processed[assembly.FullName] = true;
                        break;
                    }
                }
            }
        }

        return processed[assembly.FullName];
    }

    static Assembly? GetReferencedAssembly(AssemblyName assemblyName, AssemblyLoadContext assemblyLoadContext)
    {
        Assembly? referencedAssembly = null;

        try
        {
            referencedAssembly = assemblyLoadContext.LoadFromAssemblyName(assemblyName);
        }
        catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException or FileLoadException) { }

        referencedAssembly ??= AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName.Name);

        return referencedAssembly;
    }

    internal static string FormatReflectionTypeLoadException(string fileName, ReflectionTypeLoadException e)
    {
        var sb = new StringBuilder($"Could not enumerate all types for '{fileName}'.");

        if (e.LoaderExceptions.Length == 0)
        {
            sb.NewLine($"Exception message: {e}");
            return sb.ToString();
        }

        var files = new List<string>();
        var sbFileLoadException = new StringBuilder();
        var sbGenericException = new StringBuilder();

        foreach (var ex in e.LoaderExceptions)
        {
            if (ex is FileLoadException { FileName: not null } loadException)
            {
                if (!files.Contains(loadException.FileName))
                {
                    files.Add(loadException.FileName);
                    sbFileLoadException.NewLine(loadException.FileName);
                }
            }
            else
            {
                sbGenericException.NewLine(ex?.ToString() ?? "Unknown exception");
            }
        }

        if (sbGenericException.Length > 0)
        {
            sb.NewLine("Exceptions:");
            sb.Append(sbGenericException);
        }

        if (sbFileLoadException.Length > 0)
        {
            sb.AppendLine();
            sb.NewLine("FileLoadExceptions:");
            sb.Append(sbFileLoadException);
        }

        return sb.ToString();
    }

    static List<FileInfo> ScanDirectoryForAssemblyFiles(string directoryToScan, bool scanNestedDirectories)
    {
        var baseDir = new DirectoryInfo(directoryToScan);
        var searchOption = scanNestedDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = FileSearchPatternsToUse
            .SelectMany(pattern => baseDir.EnumerateFiles(pattern, searchOption))
            .ToList();
        return files;
    }

    bool IsExcluded(string assemblyName) => assembliesToSkip.Contains(assemblyName) || DefaultAssemblyExclusions.Contains(assemblyName);

    // The parameter and return types of this method are deliberately using the most concrete types
    // to avoid unnecessary allocations
    List<Type> FilterAllowedTypes(Type[] types)
    {
        // assume the majority of types will be allowed to preallocate the list
        var allowedTypes = new List<Type>(types.Length);
        foreach (var typeToAdd in types)
        {
            if (!IsAllowedType(typeToAdd))
            {
                continue;
            }

            allowedTypes.Add(typeToAdd);
        }

        return allowedTypes;
    }

    bool IsAllowedType(Type type) =>
        type is { IsValueType: false } &&
        Attribute.GetCustomAttribute(type, typeof(CompilerGeneratedAttribute), false) is null &&
        !typesToSkip.Contains(type);

    void AddTypesToResult(Assembly assembly, AssemblyScannerResults results)
    {
        try
        {
            //will throw if assembly cannot be loaded
            var types = assembly.GetTypes();
            results.Types.AddRange(FilterAllowedTypes(types));
        }
        catch (ReflectionTypeLoadException e)
        {
            results.ErrorsThrownDuringScanning = true;

            var errorMessage = FormatReflectionTypeLoadException(assembly.FullName ?? "Unknown assembly", e);
            if (ThrowExceptions)
            {
                throw new Exception(errorMessage);
            }

            LogManager.GetLogger<AssemblyScanner>().Warn(errorMessage);
            results.Types.AddRange(FilterAllowedTypes(e.Types.Where(t => t is not null).ToArray()!));
        }

        results.Assemblies.Add(assembly);
    }

    bool ShouldScanDependencies(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return false;
        }

        var assemblyName = assembly.GetName();

        if (AssemblyValidator.IsAssemblyToSkip(assemblyName))
        {
            return false;
        }

        // AssemblyName.Name is without the file extension
        return !IsExcluded(assemblyName.Name ?? string.Empty);
    }

    internal bool ScanNestedDirectories;
    readonly Assembly? assemblyToScan;
    readonly string? baseDirectoryToScan;
    HashSet<Type> typesToSkip = [];
    HashSet<string> assembliesToSkip = new(StringComparer.OrdinalIgnoreCase);

    static readonly string[] FileSearchPatternsToUse =
    {
        "*.dll", "*.exe"
    };

    static readonly FrozenSet<string> DefaultAssemblyExclusions = new[]
    {
        // NSB Build-Dependencies
        "nunit",
        "nunit.framework",
        "nunit.applicationdomain",
        "nunit.engine",
        "nunit.engine.api",
        "nunit.engine.core",

        // NSB OSS Dependencies
        "nlog",
        "newtonsoft.json",
        "common.logging",
        "nhibernate",

        // Raven
        "raven.client",
        "raven.abstractions",

        // Azure host process, which is typically referenced for ease of deployment but should not be scanned
        "NServiceBus.Hosting.Azure.HostProcess.exe",

        // And other windows azure stuff
        "Microsoft.WindowsAzure"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}