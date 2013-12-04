namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class AssemblyScannerTests
    {
        static string testDllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestDlls");

        [Test]
        public void System_assemblies_should_be_excluded()
        {
            Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(typeof(string).Assembly.Location));
            Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(typeof(Uri).Assembly.Location));
        }

        [Test]
        public void Non_system_assemblies_should_be_included()
        {
            Assert.IsFalse(AssemblyScanner.IsRuntimeAssembly(GetType().Assembly.Location));
        }

        [TestFixture]
        public class When_scanning_top_level_only
        {
            static string baseDirectoryToScan = Path.Combine(Path.GetTempPath(), "empty");
            static string someSubDirectory = Path.Combine(baseDirectoryToScan, "subDir");

            AssemblyScannerResults results;

            [SetUp]
            public void Context()
            {
                Directory.CreateDirectory(baseDirectoryToScan);
                Directory.CreateDirectory(someSubDirectory);

                var dllFilePath = Path.Combine(someSubDirectory, "NotAProper.dll");
                File.WriteAllText(dllFilePath, "This is not a proper DLL");

                results = new AssemblyScanner(baseDirectoryToScan)
                          {
                              IncludeAppDomainAssemblies = true,
                              ScanNestedDirectories = false
                          }
                    .GetScannableAssemblies();
            }

            [TearDown]
            public void TearDown()
            {
                if (Directory.Exists(baseDirectoryToScan))
                    Directory.Delete(baseDirectoryToScan, true);
            }
            
            [Test]
            public void should_not_find_assembly_in_subdir()
            {
                var allEncounteredFileNames =
                    results.Assemblies.Select(a => a.CodeBase)
                           .Concat(results.SkippedFiles.Select(s => s.FilePath))
                           .ToList();

                Assert.That(allEncounteredFileNames.Any(f => f.Contains("NotAProper.dll")),
                    Is.False, "Did not expect to find NotAProper.dll among all encountered files because it resides in a subdir");
            }
        }
        
        [TestFixture]
        public class When_scanning_for_dlls_only
        {
            static readonly string BaseDirectoryToScan = Path.Combine(Path.GetTempPath(), "empty");

            AssemblyScannerResults results;

            [SetUp]
            public void Context()
            {
                Directory.CreateDirectory(BaseDirectoryToScan);

                var dllFilePath = Path.Combine(BaseDirectoryToScan, "NotAProper.exe");
                File.WriteAllText(dllFilePath, "This is not a proper EXE");

                results = new AssemblyScanner(BaseDirectoryToScan)
                          {
                              IncludeAppDomainAssemblies = true,
                              IncludeExesInScan = false,
                          }
                    .GetScannableAssemblies();
            }

            [TearDown]
            public void TearDown()
            {
                if (Directory.Exists(BaseDirectoryToScan))
                    Directory.Delete(BaseDirectoryToScan, true);
            }
            
            [Test]
            public void should_not_find_assembly_in_subdir()
            {
                var allEncounteredFileNames =
                    results.Assemblies.Select(a => a.CodeBase)
                           .Concat(results.SkippedFiles.Select(s => s.FilePath))
                           .ToList();

                Assert.That(allEncounteredFileNames.Any(f => f.Contains("NotAProper.exe")),
                    Is.False, "Did not expect to find NotAProper.dll among all encountered files because it resides in a subdir");
            }
        }

        [TestFixture]
        public class When_told_to_scan_app_domain
        {
            AssemblyScannerResults results;

            [SetUp]
            public void Context()
            {
                var someDir = Path.Combine(Path.GetTempPath(), "empty");
                Directory.CreateDirectory(someDir);

                results = new AssemblyScanner(someDir)
                          {
                              IncludeAppDomainAssemblies = true,
                          }
                    .GetScannableAssemblies();
            }

            class SomeHandlerThatEnsuresThatWeKeepReferencingNsbCore : IHandleMessages<string>
            {
                public void Handle(string message)
                {
                }
            }

            [Test]
            public void Should_use_AppDomain_Assemblies_if_flagged()
            {
                var collection = results.Assemblies.Select(a => a.GetName().Name).ToArray();
             
                CollectionAssert.Contains(collection, "NServiceBus.Core.Tests");
            }
        }

        [TestFixture]
        public class When_inclusion_predicate_is_used
        {
            AssemblyScannerResults results;
            List<SkippedFile> skippedFiles;

            [SetUp]
            public void Context()
            {
                var assemblyScanner = new AssemblyScanner(testDllDirectory)
                                      {
                                          AssembliesToInclude = new List<string>
                                                                {
                                                                    "NServiceBus.Core.Tests.dll"
                                                                }
                                      };
                results = assemblyScanner
                    .GetScannableAssemblies();

                skippedFiles = results.SkippedFiles;
            }

            [Test]
            public void only_files_explicitly_included_are_returned()
            {
                Assert.That(results.Assemblies, Has.Count.EqualTo(1));
                Assert.That(results.Errors, Has.Count.EqualTo(0));
                Assert.That(skippedFiles, Has.Count.GreaterThan(0));

                Assert.That(results.Assemblies.Single().GetName().Name, Is.EqualTo("NServiceBus.Core.Tests"));
            }
        }

        [TestFixture]
        public class When_exclusion_predicate_is_used
        {
            AssemblyScannerResults results;
            List<SkippedFile> skippedFiles;

            [SetUp]
            public void Context()
            {
                results = new AssemblyScanner(testDllDirectory)
                          {
                              AssembliesToSkip = new List<string> { "Rebus.dll" }
                          }
                    .GetScannableAssemblies();

                skippedFiles = results.SkippedFiles;
            }

            [Test]
            public void no_files_explicitly_excluded_are_returned()
            {
                var explicitlySkippedDll = skippedFiles.FirstOrDefault(s => s.FilePath.Contains("Rebus.dll"));

                Assert.That(explicitlySkippedDll, Is.Not.Null);
                Assert.That(explicitlySkippedDll.SkipReason, Contains.Substring("File was explicitly excluded from scanning"));
            }
        }

        [TestFixture]
        public class When_directory_is_scanned
        {
            AssemblyScannerResults results;
            List<SkippedFile> skippedFiles;

            [SetUp]
            public void Context()
            {
                var assemblyScanner = new AssemblyScanner(testDllDirectory)
                {
                    IncludeAppDomainAssemblies = false
                };
                assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);

                results = assemblyScanner
                    .GetScannableAssemblies();

                skippedFiles = results.SkippedFiles;
            }

            [Test]
            public void non_dotnet_files_are_skipped()
            {
                var notProperDotNetDlls = new[]
                                          {
                                              "libzmq-v120-mt-3_2_3.dll",
                                              "Tail.exe",
                                              "some_random.dll",
                                              "some_random.exe",
                                          };

                foreach (var notProperDll in notProperDotNetDlls)
                {
                    var skippedFile = skippedFiles.FirstOrDefault(f => f.FilePath.Contains(notProperDll));

                    if (skippedFile == null)
                    {
                        throw new AssertionException(string.Format("Could not find skipped file matching {0}", notProperDll));
                    }

                    Assert.That(skippedFile.SkipReason, Contains.Substring("not a .NET assembly"));
                }
            }

            [Test]
            [Explicit("TODO: re-enable when we make message scanning lazy #1617")]
            public void assemblies_without_nsb_reference_are_skipped()
            {
                var skippedFile = skippedFiles.FirstOrDefault(f => f.FilePath.Contains("Rebus.dll"));

                if (skippedFile == null)
                {
                    throw new AssertionException(string.Format("Could not find skipped file matching {0}", "Rebus.dll"));
                }
                Assert.That(skippedFile.SkipReason,
                    Contains.Substring("Assembly does not reference at least one of the must referenced assemblies"));
            }

            [Test]
            public void dll_with_message_handlers_gets_loaded()
            {
                //TODO: change back to "Has.Count.EqualTo(1)" when we make message scanning lazy #1617"
                Assert.That(results.Assemblies, Has.Count.EqualTo(2));
                Assert.That(results.Errors, Has.Count.EqualTo(0));

                var containsHandlers = "NServiceBus.Core.Tests"; //< assembly name, not file name
                var assembly = results.Assemblies
                                      .FirstOrDefault(a => a.GetName().Name.Contains(containsHandlers));

                if (assembly == null)
                {
                    throw new AssertionException(string.Format("Could not find loaded assembly matching {0}", containsHandlers));
                }
            }
        }
    }
}