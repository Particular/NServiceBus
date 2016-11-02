namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using Hosting.Helpers;
    using Microsoft.CSharp;
    using Mono.Cecil;
    using NUnit.Framework;

    [TestFixture]
    public class AssemblyScannerTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupDynamicAssembliesIfNecessary();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupDynamicAssembliesIfNecessary();
        }

        [Test]
        public void System_assemblies_should_be_excluded()
        {
            Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(typeof(string).Assembly.Location));
            Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(typeof(Uri).Assembly.Location));
            Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(new AssemblyName("mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e, Retargetable=Yes")));
        }

        [Test]
        public void Non_system_assemblies_should_be_included()
        {
            Assert.IsFalse(AssemblyScanner.IsRuntimeAssembly(GetType().Assembly.Location));
        }


        [Test]
        public void ReferencesNServiceBus_requires_binding_redirect()
        {
            var combine = Path.Combine(GetTestAssemblyDirectory(), "AssemblyWithRefToSN.dll");
            Assert.IsTrue(AssemblyScanner.ReferencesNServiceBus(combine));
        }

        [Test]
        public void ReferencesNServiceBus_returns_false_for_no_reference()
        {
            var combine = Path.Combine(GetTestAssemblyDirectory(), "dotNet.dll");
            Assert.IsFalse(AssemblyScanner.ReferencesNServiceBus(combine));
        }

        [Test, RunInApplicationDomain]
        public void Assemblies_with_direct_reference_are_included()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core.dll");
            var assemblyWithReference = new DynamicAssembly("AssemblyWithReference.dll", new[]
            {
                busAssembly
            });

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory);
            scanner.CoreAssemblyName = busAssembly.DynamicName;

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(assemblyWithReference));
            Assert.IsTrue(result.Assemblies.Contains(busAssembly));
            Assert.AreEqual(2, result.Assemblies.Count);
        }

        [Test, RunInApplicationDomain]
        public void Assemblies_with_no_reference_are_excluded()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            var assemblyWithReference = new DynamicAssembly("AssemblyWithReference", new[]
            {
                busAssembly
            });
            var assemblyWithoutReference = new DynamicAssembly("AssemblyWithoutReference");

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory);
            scanner.CoreAssemblyName = busAssembly.DynamicName;

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(assemblyWithReference));
            Assert.IsTrue(result.Assemblies.Contains(busAssembly));
            Assert.IsFalse(result.Assemblies.Contains(assemblyWithoutReference));
            Assert.AreEqual(2, result.Assemblies.Count);
        }

        [Test, RunInApplicationDomain]
        public void Assemblies_which_reference_older_nsb_version_are_included()
        {
            var busAssemblyV2 = new DynamicAssembly("Fake.NServiceBus.Core", version: new Version(2, 0, 0), fakeIdentity: true);
            var assemblyReferencesV2 = new DynamicAssembly("AssemblyWithReference2", new[]
            {
                busAssemblyV2
            });
            var busAssemblyV1 = new DynamicAssembly("Fake.NServiceBus.Core", version: new Version(1, 0, 0), fakeIdentity: true);
            var assemblyReferencesV1 = new DynamicAssembly("AssemblyWithReference1", new[]
            {
                busAssemblyV1
            });

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory);
            scanner.ThrowExceptions = false;
            scanner.CoreAssemblyName = busAssemblyV2.Name;

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(busAssemblyV2));
            Assert.IsTrue(result.Assemblies.Contains(assemblyReferencesV1));
            Assert.IsTrue(result.Assemblies.Contains(assemblyReferencesV2));
            Assert.AreEqual(3, result.Assemblies.Count);
        }

        [Test, RunInApplicationDomain]
        public void Assemblies_with_transitive_reference_are_include()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            var assemblyC = new DynamicAssembly("C", new[]
            {
                busAssembly
            });
            var assemblyB = new DynamicAssembly("B", new[]
            {
                assemblyC
            });
            var assemblyA = new DynamicAssembly("A", new[]
            {
                assemblyB
            });
            var assemblyD = new DynamicAssembly("D", new[]
            {
                assemblyB
            });

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory);
            scanner.CoreAssemblyName = busAssembly.DynamicName;

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(assemblyA));
            Assert.IsTrue(result.Assemblies.Contains(assemblyB));
            Assert.IsTrue(result.Assemblies.Contains(assemblyC));
            Assert.IsTrue(result.Assemblies.Contains(assemblyD));
            Assert.IsTrue(result.Assemblies.Contains(busAssembly));
            Assert.AreEqual(5, result.Assemblies.Count);
        }

        public static string GetTestAssemblyDirectory()
        {
            var directoryName = GetAssemblyDirectory();
            return Path.Combine(directoryName, "TestDlls");
        }

        static string GetAssemblyDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        static void CleanupDynamicAssembliesIfNecessary()
        {
            if (!AppDomainRunner.IsInTestAppDomain)
            {
                Directory.Delete(DynamicAssembly.TestAssemblyDirectory, true);
            }
        }

        [DebuggerDisplay("Name = {Name}, DynamicName = {DynamicName}, Namespace = {Namespace}, FileName = {FileName}")]
        class DynamicAssembly
        {
            public DynamicAssembly(string nameWithoutExtension, DynamicAssembly[] references = null, Version version = null, bool fakeIdentity = false)
            {
                if (version == null)
                {
                    version = new Version(1, 0, 0, 0);
                }

                if (references == null)
                {
                    references = new DynamicAssembly[0];
                }

                Name = nameWithoutExtension;
                Namespace = nameWithoutExtension;
                FileName = $"{Namespace}{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}{Interlocked.Increment(ref dynamicAssemblyId)}.dll";
                DynamicName = Path.GetFileNameWithoutExtension(FileName);

                var builder = new StringBuilder();
                builder.AppendLine("using System.Reflection;");
                builder.AppendLine($"[assembly: AssemblyVersion(\"{version}\")]");
                builder.AppendLine($"[assembly: AssemblyFileVersion(\"{version}\")]");

                builder.AppendFormat("namespace {0} {{ ", Namespace);

                var provider = new CSharpCodeProvider();
                var param = new CompilerParameters(new string[]
                {
                }, FileName);
                param.GenerateExecutable = false;
                param.GenerateInMemory = false;
                param.OutputAssembly = FilePath = Path.Combine(TestAssemblyDirectory, FileName);
                param.TempFiles = new TempFileCollection(TestAssemblyDirectory, false);

                foreach (var reference in references)
                {
                    builder.AppendLine($"using {reference.Namespace};");
                    param.ReferencedAssemblies.Add(reference.FilePath);
                }

                builder.AppendLine("public class Foo { public Foo() {");
                foreach (var reference in references)
                {
                    builder.AppendLine($"new {reference.Namespace}.Foo();");
                }
                builder.AppendLine("} } }");

                var result = provider.CompileAssemblyFromSource(param, builder.ToString());
                ThrowIfCompilationWasNotSuccessful(result);

                provider.Dispose();

                if (fakeIdentity)
                {
                    var reader = AssemblyDefinition.ReadAssembly(FilePath);
                    reader.Name.Name = nameWithoutExtension;
                    reader.MainModule.Name = nameWithoutExtension;
                    reader.Write(FilePath);
                }

                Assembly = result.CompiledAssembly;
            }

            public string Namespace { get; }

            public string Name { get; }

            public string DynamicName { get; }

            public string FileName { get; }

            public string FilePath { get; }

            public Assembly Assembly { get; }

            public static string TestAssemblyDirectory => GetTestAssemblyDirectory();

            static string GetTestAssemblyDirectory()
            {
                var directoryName = GetAssemblyDirectory();
                var directory = Path.Combine(directoryName, "assemblyscannerfiles");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                return directory;
            }

            static string GetAssemblyDirectory()
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }

            static void ThrowIfCompilationWasNotSuccessful(CompilerResults results)
            {
                if (results.Errors.HasErrors)
                {
                    var errors = new StringBuilder("Compiler Errors :\r\n");
                    foreach (CompilerError error in results.Errors)
                    {
                        errors.AppendFormat("Line {0},{1}\t: {2}\n",
                            error.Line, error.Column, error.ErrorText);
                    }
                    throw new Exception(errors.ToString());
                }
            }

            public static implicit operator Assembly(DynamicAssembly dynamicAssembly) => dynamicAssembly.Assembly;

            static long dynamicAssemblyId;
        }
    }
}