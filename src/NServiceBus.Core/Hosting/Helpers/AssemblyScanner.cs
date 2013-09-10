namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Helpers for assembly scanning operations
    /// </summary>
    public class AssemblyScanner
    {
        readonly List<string> assembliesToSkip = new List<string>();
        readonly List<string> assembliesToInclude = new List<string>();
        readonly string baseDirectoryToScan;

        bool includeAppDomainAssemblies;

        public AssemblyScanner(string baseDirectoryToScan)
        {
            this.baseDirectoryToScan = baseDirectoryToScan;
        }

        public AssemblyScanner()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public AssemblyScanner IncludeAppDomainAssemblies()
        {
            includeAppDomainAssemblies = true;
            Console.WriteLine(@"In the app domain:

{0}",
                              string.Join(Environment.NewLine, AppDomain.CurrentDomain.GetAssemblies()
                                                                        .Select(a => a.GetName()
                                                                                      .Name)));
            return this;
        }

        /// <summary>
        /// Traverses the specified base directory including all subdirectories, generating a list of assemblies that can be
        /// scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
        /// Scanned files may be skipped when they're either not a .NET assembly, or if a reflection-only load of the .NET assembly
        /// reveals that it does not reference NServiceBus.
        /// </summary>
        //[DebuggerNonUserCode]
        public AssemblyScannerResults GetScannableAssemblies()
        {
            var baseDir = new DirectoryInfo(baseDirectoryToScan);

            var assemblyFiles = baseDir.GetFiles("*.dll", SearchOption.AllDirectories)
                                       .Union(baseDir.GetFiles("*.exe", SearchOption.AllDirectories))
                                       .ToList();

            var results = new AssemblyScannerResults();

            if (includeAppDomainAssemblies)
            {
                var matchingAssembliesFromAppDomain = AppDomain.CurrentDomain
                                                           .GetAssemblies()
                                                           .Where(assembly => IsIncluded(assembly.GetName().Name))
                                                           .ToArray();

                results.Assemblies.AddRange(matchingAssembliesFromAppDomain);
            }

            foreach (var assemblyFile in assemblyFiles)
            {
                Assembly assembly;

                try
                {
                    if (!IsIncluded(assemblyFile.Name))
                    {
                        results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "Explicitly excluded from scanning"));
                        continue;
                    }

                    if (!Image.IsAssembly(assemblyFile.FullName))
                    {
                        results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "Is not a .NET assembly"));
                        continue;
                    }

                    if (!AssemblyReferencesNServiceBus(assemblyFile))
                    {
                        results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "Does not reference NServiceBus and thus cannot contain any handlers"));
                        continue;
                    }

                    assembly = Assembly.LoadFrom(assemblyFile.FullName);
                }
                catch (BadImageFormatException badImageFormatException)
                {
                    var errorMessage = string.Format("Could not load {0}. Consider using 'Configure.With(AllAssemblies.Except(\"{1}\"))' to tell NServiceBus not to load this file.", assemblyFile.FullName, assemblyFile.Name);
                    var error = new ErrorWhileScanningAssemblies(badImageFormatException, errorMessage);
                    results.Errors.Add(error);
                    continue;
                }

                try
                {
                    //will throw if assembly cannot be loaded
                    assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    var sb = new StringBuilder();
                    sb.Append(string.Format("Could not scan assembly: {0}. Exception message {1}.", assemblyFile.FullName, e));
                    if (e.LoaderExceptions.Any())
                    {
                        sb.Append(Environment.NewLine + "Scanned type errors: ");
                        foreach (var ex in e.LoaderExceptions)
                            sb.Append(Environment.NewLine + ex.Message);
                    }
                    var error = new ErrorWhileScanningAssemblies(e, sb.ToString());
                    results.Errors.Add(error);
                    continue;
                }

                results.Assemblies.Add(assembly);
            }

            return results;
        }

        bool AssemblyReferencesNServiceBus(FileInfo assemblyFile)
        {
            var lightLoad = Assembly.ReflectionOnlyLoadFrom(assemblyFile.FullName);
            var referencedAssemblies = lightLoad.GetReferencedAssemblies();

            var nameOfAssemblyDefiningHandlersInterface =
                typeof(IHandleMessages<>).Assembly.GetName().Name;

            return referencedAssemblies
                .Any(a => a.Name == nameOfAssemblyDefiningHandlersInterface);
        }

        static readonly IEnumerable<string> DefaultAssemblyInclusionOverrides = new[] { "nservicebus." };

        static readonly IEnumerable<string> DefaultAssemblyExclusions
            = new[]
              {

                  "system.", 
                  "mscorlib.", 
                  
                  // NSB Build-Dependencies
                  "nunit.", "pnunit.", "rhino.mocks.","XsdGenerator.",
                 
                  // NSB OSS Dependencies
                  "rhino.licensing.", "bouncycastle.crypto",
                  "magnum.", "interop.", "nlog.", "newtonsoft.json.",
                  "common.logging.", "topshelf.",
                  "Autofac.", "log4net.","nhibernate.", 

                  // Raven
                  "raven.server", "raven.client", "raven.munin.",
                  "raven.storage.", "raven.abstractions.", "raven.database",
                  "esent.interop", "asyncctplibrary.", "lucene.net.", 
                  "icsharpcode.nrefactory", "spatial4n.core",

                  // Azure host process, which is typically referenced for ease of deployment but should not be scanned
                  "NServiceBus.Hosting.Azure.HostProcess.exe",

                  // And other windows azure stuff
                  "Microsoft.WindowsAzure.",

                  // SQLite unmanaged DLLs that cause BadImageFormatException's
                  "sqlite3.dll", "SQLite.Interop.dll"
              };

        static readonly IEnumerable<string> DefaultTypeExclusions
            = new string[]
              {
                  // defaultAssemblyExclusions will merged inn; specify additional ones here 
              };

        /// <summary>
        /// Determines whether the specified assembly name or file name can be included, given the set up include/exclude
        /// patterns and default include/exclude patterns
        /// </summary>
        bool IsIncluded(string assemblyNameOrFileName)
        {
            var isExcludedByDefault = DefaultAssemblyExclusions.Any(exclusion => IsMatch(exclusion, assemblyNameOrFileName));
            var isExplicitlyExcluded = assembliesToSkip.Any(excluded => IsMatch(excluded, assemblyNameOrFileName));

            if (isExcludedByDefault || isExplicitlyExcluded)
                return false;

            var noAssembliesWereExplicitlyIncluded = !assembliesToInclude.Any();
            var isAlwaysIncludedByDefault = DefaultAssemblyInclusionOverrides.Any(o => IsMatch(o, assemblyNameOrFileName));
            var isExplicitlyIncluded = assembliesToInclude.Any(included => IsMatch(included, assemblyNameOrFileName));

            return noAssembliesWereExplicitlyIncluded
                   || isAlwaysIncludedByDefault
                   || isExplicitlyIncluded;
        }

        static bool IsMatch(string expression, string scopedNameOrFileName)
        {
            if (DistillLowerAssemblyName(scopedNameOrFileName).StartsWith(expression.ToLower()))
                return true;

            if (DistillLowerAssemblyName(expression).TrimEnd('.') == DistillLowerAssemblyName(scopedNameOrFileName))
                return true;

            return false;
        }

        public static bool IsAllowedType(Type type)
        {
            return !type.IsValueType
                   && DefaultTypeExclusions.Union(DefaultAssemblyExclusions)
                                           .Any(exclusion => IsMatch(exclusion, type.FullName));
        }

        static string DistillLowerAssemblyName(string assemblyOrFileName)
        {
            var lowerAssemblyName = assemblyOrFileName.ToLowerInvariant();
            if (lowerAssemblyName.EndsWith(".dll"))
            {
                lowerAssemblyName = lowerAssemblyName.Substring(0, lowerAssemblyName.Length - 4);
            }
            return lowerAssemblyName;
        }

        public AssemblyScanner IncludeAssemblies(IEnumerable<string> assembliesToAddToListOfIncludedAssemblies)
        {
            if (assembliesToAddToListOfIncludedAssemblies != null)
            {
                assembliesToInclude.AddRange(assembliesToAddToListOfIncludedAssemblies);
            }
            return this;
        }

        public AssemblyScanner SkipAssemblies(IEnumerable<string> assembliesToAddToListOfSkippedAssemblies)
        {
            if (assembliesToAddToListOfSkippedAssemblies != null)
            {
                assembliesToSkip.AddRange(assembliesToAddToListOfSkippedAssemblies);
            }
            return this;
        }

        // Code kindly provided by the mono project: https://github.com/jbevain/mono.reflection/blob/master/Mono.Reflection/Image.cs
        // Image.cs
        //
        // Author:
        //   Jb Evain (jbevain@novell.com)
        //
        // (C) 2009 - 2010 Novell, Inc. (http://www.novell.com)
        //
        // Permission is hereby granted, free of charge, to any person obtaining
        // a copy of this software and associated documentation files (the
        // "Software"), to deal in the Software without restriction, including
        // without limitation the rights to use, copy, modify, merge, publish,
        // distribute, sublicense, and/or sell copies of the Software, and to
        // permit persons to whom the Software is furnished to do so, subject to
        // the following conditions:
        //
        // The above copyright notice and this permission notice shall be
        // included in all copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
        // EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
        // MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
        // NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
        // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
        // OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
        // WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
        class Image : IDisposable
        {
            readonly long positionWhenCreated;
            readonly Stream stream;

            public static bool IsAssembly(string file)
            {
                if (file == null)
                    throw new ArgumentNullException("file");

                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return IsAssembly(stream);
                }
            }

            static bool IsAssembly(Stream stream)
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");
                if (!stream.CanRead)
                    throw new ArgumentException("Can not read from stream");
                if (!stream.CanSeek)
                    throw new ArgumentException("Can not seek in stream");

                using (var image = new Image(stream))
                {
                    return image.IsManagedAssembly();
                }
            }

            Image(Stream stream)
            {
                this.stream = stream;
                positionWhenCreated = stream.Position;
                this.stream.Position = 0;
            }

            bool IsManagedAssembly()
            {
                if (stream.Length < 318)
                    return false;
                if (ReadUInt16() != 0x5a4d)
                    return false;
                if (!Advance(58))
                    return false;
                if (!MoveTo(ReadUInt32()))
                    return false;
                if (ReadUInt32() != 0x00004550)
                    return false;
                if (!Advance(20))
                    return false;
                if (!Advance(ReadUInt16() == 0x20b ? 222 : 206))
                    return false;

                return ReadUInt32() != 0;
            }

            bool Advance(int length)
            {
                if (stream.Position + length >= stream.Length)
                    return false;

                stream.Seek(length, SeekOrigin.Current);
                return true;
            }

            bool MoveTo(uint position)
            {
                if (position >= stream.Length)
                    return false;

                stream.Position = position;
                return true;
            }

            void IDisposable.Dispose()
            {
                stream.Position = positionWhenCreated;
            }

            ushort ReadUInt16()
            {
                return (ushort)(stream.ReadByte()
                                | (stream.ReadByte() << 8));
            }

            uint ReadUInt32()
            {
                return (uint)(stream.ReadByte()
                              | (stream.ReadByte() << 8)
                              | (stream.ReadByte() << 16)
                              | (stream.ReadByte() << 24));
            }
        }
    }
}