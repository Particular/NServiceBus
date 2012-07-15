using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NServiceBus.Hosting.Helpers
{
    /// <summary>
    /// Helpers for assembly scanning operations
    /// </summary>
    public class AssemblyScanner
    {
        /// <summary>
        /// Gets a list of assemblies that can be scanned and a list of errors that occurred while scanning.
        /// 
        /// </summary>
        /// <returns></returns>
        [DebuggerNonUserCode] //so that exceptions don't jump at the developer debugging their app
        public static AssemblyScannerResults GetScannableAssemblies()
        {
            var assemblyFiles = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.dll", SearchOption.AllDirectories)
                .Union(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.exe", SearchOption.AllDirectories));
            var results = new AssemblyScannerResults();
            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyFile.FullName);

                    //will throw if assembly cannot be loaded
                    assembly.GetTypes();
                    results.Assemblies.Add(assembly);
                }
                catch (BadImageFormatException bif)
                {
                    var error = new ErrorWhileScanningAssemblies(bif, "Could not load " + assemblyFile.FullName +
                        ". Consider using 'Configure.With(AllAssemblies.Except(\"" + assemblyFile.Name + "\"))' to tell NServiceBus not to load this file.");
                    results.Errors.Add(error);
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
                }
            }
            return results;
        }
    }
}