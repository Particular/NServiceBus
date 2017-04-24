namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
            if (!AppDomainRunner.IsInTestAppDomain)
            {
                AppDomainRunner.DataStore.Set("TestDirectory", TestContext.CurrentContext.TestDirectory);

                if (Directory.Exists(DynamicAssembly.TestAssemblyDirectory))
                {
                    Directory.Delete(DynamicAssembly.TestAssemblyDirectory, true);
                }

                Directory.CreateDirectory(DynamicAssembly.TestAssemblyDirectory);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (!AppDomainRunner.IsInTestAppDomain)
            {
                Directory.Delete(DynamicAssembly.TestAssemblyDirectory, true);
            }
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
            var combine = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "AssemblyWithRefToSN.dll");
            var scanner = new AssemblyScanner();

            Assert.IsTrue(scanner.ReferencesNServiceBus(combine, new Dictionary<string, bool>()));
        }

        [Test]
        public void ReferencesNServiceBus_returns_false_for_no_reference()
        {
            var combine = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "dotNet.dll");
            var scanner = new AssemblyScanner();

            Assert.IsFalse(scanner.ReferencesNServiceBus(combine, new Dictionary<string, bool>()));
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

        [Test, RunInApplicationDomain]
        public void AppDomainAssemblies_are_included_when_enabling_ScanAppDomainAssemblies()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            Assembly.LoadFrom(busAssembly.FilePath);

            var scanner = new AssemblyScanner(); // don't scan the dynamic assembly folder
            scanner.CoreAssemblyName = busAssembly.DynamicName;
            scanner.ScanAppDomainAssemblies = true;

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(busAssembly));
        }

        [Test, RunInApplicationDomain]
        public void Throw_exception_on_assembly_loading_conflicts()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");

            Directory.CreateDirectory(Path.Combine(DynamicAssembly.TestAssemblyDirectory, "subdir"));
            var destFileName = Path.Combine(DynamicAssembly.TestAssemblyDirectory, "subdir", busAssembly.FileName);
            File.Copy(busAssembly.FilePath, destFileName);

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory);
            scanner.ScanNestedDirectories = true;
            scanner.CoreAssemblyName = busAssembly.DynamicName;

            var exception = Assert.Throws<Exception>(() => scanner.GetScannableAssemblies());
            Assert.IsInstanceOf<FileLoadException>(exception.InnerException);
        }

        [Test, RunInApplicationDomain]
        public void Ignore_assembly_loading_errors_when_disabling_ThrowExceptions()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");

            Directory.CreateDirectory(Path.Combine(DynamicAssembly.TestAssemblyDirectory, "subdir"));
            var destFileName = Path.Combine(DynamicAssembly.TestAssemblyDirectory, "subdir", busAssembly.FileName);
            File.Copy(busAssembly.FilePath, destFileName);
            Assembly.LoadFrom(destFileName);

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory);
            scanner.ScanAppDomainAssemblies = true;
            scanner.CoreAssemblyName = busAssembly.DynamicName;
            scanner.ThrowExceptions = false;

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(busAssembly));
        }

        [Test, RunInApplicationDomain]
        public void Include_child_type_even_if_only_handler_for_base_exists()
        {
            var messages =
@"
public interface IBaseEvent
{
}

public interface IInheritedEvent : IBaseEvent
{
}
";

            var handler =
@"
using NServiceBus;
using System.Threading.Tasks;

class InterfaceMessageHandler : IHandleMessages<IBaseEvent>
{
    public Task Handle(IBaseEvent message, IMessageHandlerContext context)
    {
        return Task.FromResult(0);
    }
}
";

            var messagesAsm = new DynamicAssembly("Fake.Messages", content: messages);
            Assembly.LoadFrom(messagesAsm.FilePath);

            var handlerAsm = new DynamicAssembly("Fake.Handler", new[] { messagesAsm }, content: handler, referenceTheCore: true);
            Assembly.LoadFrom(handlerAsm.FilePath);

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory);

            var result = scanner.GetScannableAssemblies();

            Assert.True(result.Types.Any(t => t.Name == "IInheritedEvent"));
        }

        [DebuggerDisplay("Name = {Name}, DynamicName = {DynamicName}, Namespace = {Namespace}, FileName = {FileName}")]
        class DynamicAssembly
        {
            public DynamicAssembly(string nameWithoutExtension, DynamicAssembly[] references = null, Version version = null, bool fakeIdentity = false, string content = null, bool referenceTheCore = false)
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

                if (referenceTheCore)
                {
                    var targetCorePath = Path.Combine(TestAssemblyDirectory, "NServiceBus.Core.dll");
                    File.Copy(Path.Combine(TestDirectory, "NServiceBus.Core.dll"), Path.Combine(TestAssemblyDirectory, "NServiceBus.Core.dll"));

                    param.ReferencedAssemblies.Add(targetCorePath);
                }

                if (content == null)
                {
                    builder.AppendLine("public class Foo { public Foo() {");
                    foreach (var reference in references)
                    {
                        builder.AppendLine($"new {reference.Namespace}.Foo();");
                    }
                    builder.AppendLine("} }");
                }
                else
                {
                    builder.AppendLine(content);
                }

                builder.AppendLine(" }");


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

            public static string TestDirectory { get; } = AppDomainRunner.DataStore.Get<string>("TestDirectory");

            public static string TestAssemblyDirectory { get; } = Path.Combine(TestDirectory, "assemblyscannerfiles");

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