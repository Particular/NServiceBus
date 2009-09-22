using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NServiceBus.Host.Internal
{
    /// <summary>
    /// Helpers for assembly scanning operations
    /// </summary>
    public class AssemblyScanner
    {
        /// <summary>
        /// Gets a list with assemblies that can be scanned
        /// </summary>
        /// <returns></returns>
        [DebuggerNonUserCode] //so that exceptions don't jump at the developer debugging their app
        public static IEnumerable<Assembly> GetScannableAssemblies()
        {
            var assemblyFiles = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.dll", SearchOption.AllDirectories)
                                                           .Union(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.exe", SearchOption.AllDirectories));


            foreach (var assemblyFile in assemblyFiles)
            {
                Assembly assembly;

                try
                {
                    assembly = Assembly.LoadFrom(assemblyFile.FullName);

                    //will throw if assembly cant be loaded
                    assembly.GetTypes();
                }

                catch (ReflectionTypeLoadException err)
                {
                    foreach (var loaderException in err.LoaderExceptions)
                    {
                        Trace.Fail("Problem with loading " + assemblyFile.FullName, loaderException.Message);
                    }

                    continue;
                }
                catch (Exception e)
                {
                    Trace.WriteLine("NServiceBus Host - assembly load failure - ignoring " + assemblyFile + " because of error: " + e);
                    continue;
                }

                yield return assembly;


            }


        }

    }
}