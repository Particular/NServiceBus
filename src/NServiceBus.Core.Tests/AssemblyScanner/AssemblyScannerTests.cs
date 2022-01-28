#if NETFRAMEWORK
namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.CodeDom.Compiler;
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
        [RunInApplicationDomain]
        public void Assemblies_with_direct_reference_are_included()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core.dll");
            var assemblyWithReference = new DynamicAssembly("AssemblyWithReference.dll", new[]
            {
                busAssembly
            });

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                ScanAppDomainAssemblies = false,
                CoreAssemblyName = busAssembly.DynamicName
            };

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(assemblyWithReference));
            Assert.AreEqual(1, result.Assemblies.Count);
        }

        [Test]
        [RunInApplicationDomain]
        public void Assemblies_with_no_reference_are_excluded()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            var assemblyWithReference = new DynamicAssembly("AssemblyWithReference", new[]
            {
                busAssembly
            });
            var assemblyWithoutReference = new DynamicAssembly("AssemblyWithoutReference");

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                ScanAppDomainAssemblies = false,
                CoreAssemblyName = busAssembly.DynamicName
            };

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(assemblyWithReference));
            Assert.IsFalse(result.Assemblies.Contains(assemblyWithoutReference));
            Assert.AreEqual(1, result.Assemblies.Count);
        }

        [Test]
        [RunInApplicationDomain]
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

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                ThrowExceptions = false,
                ScanAppDomainAssemblies = false,
                CoreAssemblyName = busAssemblyV2.Name
            };

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(assemblyReferencesV1));
            Assert.IsTrue(result.Assemblies.Contains(assemblyReferencesV2));
            Assert.AreEqual(2, result.Assemblies.Count);
        }

        [Test]
        [RunInApplicationDomain]
        public void Assemblies_with_transitive_references_are_included()
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

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                ScanAppDomainAssemblies = false,
                CoreAssemblyName = busAssembly.DynamicName
            };

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(assemblyA));
            Assert.IsTrue(result.Assemblies.Contains(assemblyB));
            Assert.IsTrue(result.Assemblies.Contains(assemblyC));
            Assert.IsTrue(result.Assemblies.Contains(assemblyD));
            Assert.IsFalse(result.Assemblies.Contains(busAssembly));
            Assert.AreEqual(4, result.Assemblies.Count);
        }

        [Test]
        [RunInApplicationDomain]
        public void Should_not_include_core_assembly_types()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            Assembly.LoadFrom(busAssembly.FilePath);

            var scanner = new AssemblyScanner
            {
                CoreAssemblyName = busAssembly.DynamicName,
                ScanAppDomainAssemblies = true,
                ScanFileSystemAssemblies = true
            }; // don't scan the dynamic assembly folder

            var result = scanner.GetScannableAssemblies();

            Assert.IsFalse(result.Assemblies.Contains(busAssembly));
            Assert.IsFalse(result.Assemblies.Any(a => a.GetName().Name.Contains("NServiceBus.Core")));
            Assert.IsFalse(result.Types.Any(t => t == typeof(Endpoint)));
        }

        [Test]
        [RunInApplicationDomain]
        public void AppDomainAssemblies_are_included_when_enabling_ScanAppDomainAssemblies()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            var appDomainAssembly = new DynamicAssembly("AppDomainAssembly", references: new[] { busAssembly });
            Assembly.LoadFrom(appDomainAssembly.FilePath);

            var scanner = new AssemblyScanner
            {
                CoreAssemblyName = busAssembly.DynamicName,
                ScanAppDomainAssemblies = true
            }; // don't scan the dynamic assembly folder

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.Assemblies.Contains(appDomainAssembly));
        }

        [Test]
        [RunInApplicationDomain]
        public void FileSystemAssemblies_are_excluded_when_disabling_ScanFileSystemAssemblies()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                CoreAssemblyName = busAssembly.DynamicName,
                ScanAppDomainAssemblies = false,
                ScanFileSystemAssemblies = false
            };

            var result = scanner.GetScannableAssemblies();

            Assert.IsFalse(result.Assemblies.Contains(busAssembly));
        }

        [Test]
        [RunInApplicationDomain]
        public void Does_not_throw_exception_when_scanning_duplicate_assemblies()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            var duplicateAssembly = new DynamicAssembly("DuplicateAssembly", references: new[] { busAssembly });

            Directory.CreateDirectory(Path.Combine(DynamicAssembly.TestAssemblyDirectory, "subdir"));
            var destFileName = Path.Combine(DynamicAssembly.TestAssemblyDirectory, "subdir", duplicateAssembly.FileName);
            // create a duplicate of the scanned assembly in a subfolder:
            File.Copy(duplicateAssembly.FilePath, destFileName);

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                ScanNestedDirectories = true,
                CoreAssemblyName = busAssembly.DynamicName
            };

            var result = scanner.GetScannableAssemblies(); // should not throw
            Assert.IsTrue(result.Assemblies.Contains(duplicateAssembly));
        }

        [Ignore("can't force an actual error")]
        [Test]
        [RunInApplicationDomain]
        public void Ignore_assembly_loading_errors_when_disabling_ThrowExceptions()
        {
            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                ScanAppDomainAssemblies = true,
                ThrowExceptions = false
            };

            var result = scanner.GetScannableAssemblies();

            Assert.IsTrue(result.ErrorsThrownDuringScanning);
        }

        [Test]
        [RunInApplicationDomain]
        public void Skipped_dlls_should_be_excluded()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            var excludedAssembly1 = new DynamicAssembly("A", new[]
            {
                busAssembly
            });
            var excludedAssembly2 = new DynamicAssembly("A", new[]
            {
                busAssembly
            });
            var includedAssembly = new DynamicAssembly("B", new[]
            {
                busAssembly
            });

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                CoreAssemblyName = busAssembly.DynamicName
            };
            scanner.AssembliesToSkip.Add(excludedAssembly1.DynamicName); // without file extension
            scanner.AssembliesToSkip.Add(excludedAssembly2.FileName); // with file extension

            var result = scanner.GetScannableAssemblies();
            Assert.That(result.SkippedFiles.Any(s => s.FilePath == excludedAssembly1.FilePath));
            Assert.That(result.SkippedFiles.Any(s => s.FilePath == excludedAssembly2.FilePath));
            Assert.That(result.Assemblies.Contains(includedAssembly.Assembly));
        }

        [Test]
        [RunInApplicationDomain]
        public void Skipped_exes_should_be_excluded()
        {
            var busAssembly = new DynamicAssembly("Fake.NServiceBus.Core");
            var excludedAssembly1 = new DynamicAssembly("A", new[]
            {
                busAssembly
            }, executable: true);
            var excludedAssembly2 = new DynamicAssembly("A", new[]
            {
                busAssembly
            }, executable: true);
            var includedAssembly = new DynamicAssembly("B", new[]
            {
                busAssembly
            }, executable: true);

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                CoreAssemblyName = busAssembly.DynamicName
            };
            scanner.AssembliesToSkip.Add(excludedAssembly1.DynamicName); // without file extension
            scanner.AssembliesToSkip.Add(excludedAssembly2.FileName); // with file extension

            var result = scanner.GetScannableAssemblies();
            Assert.That(result.SkippedFiles.Any(s => s.FilePath == excludedAssembly1.FilePath));
            Assert.That(result.SkippedFiles.Any(s => s.FilePath == excludedAssembly2.FilePath));
            Assert.That(result.Assemblies.Contains(includedAssembly.Assembly));
        }

        [Test]
        [RunInApplicationDomain]
        public void Should_not_include_child_type_if_only_handler_for_base_exists()
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

            var handlerAsm = new DynamicAssembly("Fake.Handler", new[]
            {
                messagesAsm
            }, content: handler, referenceTheCore: true);
            Assembly.LoadFrom(handlerAsm.FilePath);

            var scanner = new AssemblyScanner(DynamicAssembly.TestAssemblyDirectory)
            {
                ScanAppDomainAssemblies = false
            };

            var result = scanner.GetScannableAssemblies();

            //Note this this is not the expected behavior. The assert will be changed to Assert.True and the test renamed as part of https://github.com/Particular/NServiceBus/issues/4634
            Assert.False(result.Types.Any(t => t.Name == "IInheritedEvent"));
        }

        [DebuggerDisplay("Name = {Name}, DynamicName = {DynamicName}, Namespace = {Namespace}, FileName = {FileName}")]
        class DynamicAssembly
        {
            public DynamicAssembly(string nameWithoutExtension, DynamicAssembly[] references = null, Version version = null, bool fakeIdentity = false, string content = null, bool referenceTheCore = false, bool executable = false)
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
                var fileExtension = executable ? "exe" : "dll";
                FileName = $"{Namespace}{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}{Interlocked.Increment(ref dynamicAssemblyId)}.{fileExtension}";
                DynamicName = Path.GetFileNameWithoutExtension(FileName);

                var builder = new StringBuilder();
                builder.AppendLine("using System.Reflection;");
                builder.AppendLine($"[assembly: AssemblyVersion(\"{version}\")]");
                builder.AppendLine($"[assembly: AssemblyFileVersion(\"{version}\")]");

                builder.AppendFormat("namespace {0} {{ ", Namespace);

                var provider = new CSharpCodeProvider();
                var param = new CompilerParameters(new string[]
                {
                }, FileName)
                {
                    GenerateExecutable = false,
                    GenerateInMemory = false,
                    OutputAssembly = FilePath = Path.Combine(TestAssemblyDirectory, FileName),
                    TempFiles = new TempFileCollection(TestAssemblyDirectory, false)
                };

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

                if (executable)
                {
                    param.GenerateExecutable = true;
                    builder.AppendLine("public static class Program { public static void Main(string[] args){} }");
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
                    using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(FilePath, new ReaderParameters
                    {
                        ReadWrite = true
                    }))
                    {
                        assemblyDefinition.Name.Name = nameWithoutExtension;
                        assemblyDefinition.MainModule.Name = nameWithoutExtension;
                        assemblyDefinition.Write();
                    }
                }

                Assembly = result.CompiledAssembly;
            }

            public string Namespace { get; }

            public string Name { get; }

            public string DynamicName { get; }

            public string FileName { get; }

            public string FilePath { get; }

            public Assembly Assembly { get; }

            public static string TestDirectory => AppDomainRunner.DataStore.Get<string>("TestDirectory");

            public static string TestAssemblyDirectory => Path.Combine(TestDirectory, "assemblyscannerfiles");

            static void ThrowIfCompilationWasNotSuccessful(CompilerResults results)
            {
                if (results.Errors.HasErrors)
                {
                    var errors = new StringBuilder($"Compiler Errors :{Environment.NewLine}");
                    foreach (CompilerError error in results.Errors)
                    {
                        errors.Append($"Line {error.Line},{error.Column}\t: {error.ErrorText}{Environment.NewLine}");
                    }
                    throw new Exception(errors.ToString());
                }
            }

            public static implicit operator Assembly(DynamicAssembly dynamicAssembly) => dynamicAssembly.Assembly;

            static long dynamicAssemblyId;
        }
    }
}
#endif
