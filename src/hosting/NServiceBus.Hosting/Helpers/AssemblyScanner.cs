using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Hosting.Helpers
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
                catch (BadImageFormatException bif)
                {
                    Console.Out.WriteLine("Could not load " + assemblyFile.FullName + 
                        ". Consider using 'Configure.With(AllAssemblies.Except(\"" + assemblyFile.Name + "\"))' to tell NServiceBus not to load this file."
                    + bif.Message);
                    continue;
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
                    Console.Out.WriteLine(sb.ToString());
                    continue;
                }
                yield return assembly;
            }
        }
    }

    internal static class AssemblyListExtensions
    {
        public static IEnumerable<Type> AllTypes(this IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
                foreach (var type in assembly.GetTypes())
                {
                    yield return type;
                }
        }


        public static IEnumerable<Type> AllTypesAssignableTo<T>(this IEnumerable<Assembly> assemblies)
        {
            return assemblies.AllTypesAssignableTo(typeof(T));
        }

        public static IEnumerable<Type> AllTypesAssignableTo(this IEnumerable<Assembly> assemblies, Type type)
        {
            return assemblies.AllTypes().Where(type.IsAssignableFrom);
        }

        public static IEnumerable<Type> AllTypesClosing(this IEnumerable<Assembly> assemblies, Type openGenericType, Type genericArg)
        {
            return assemblies.AllTypes().Where(type => type.GetGenericallyContainedType(openGenericType, genericArg) != null);
        }


    }
}