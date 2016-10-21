namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class AssemblyScannerTests
    {
        public static string GetTestAssemblyDirectory()
        {
            var directoryName = GetAssemblyDirectory();
            return Path.Combine(directoryName, "TestDlls");
        }

        public static string GetAssemblyDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
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
        public void ReferencesNServiceBus_returns_true_for_indirect_reference()
        {
            var combine = Path.Combine(GetTestAssemblyDirectory(),"AssemblyWithNoDirectReference.dll");
            Assert.IsTrue(AssemblyScanner.ReferencesNServiceBus(combine, new Dictionary<AssemblyName, AssemblyName>()));
        }

        [Test]
        public void ReferencesNServiceBus_requires_binding_redirect()
        {
            var combine = Path.Combine(GetTestAssemblyDirectory(), "AssemblyWithRefToSN.dll");
            Assert.IsTrue(AssemblyScanner.ReferencesNServiceBus(combine, new Dictionary<AssemblyName, AssemblyName>()));
        }

        [Test]
        public void ReferencesNServiceBus_returns_true_for_direct_reference()
        {
            var combine = Path.Combine(GetTestAssemblyDirectory(), "AssemblyWithReference.dll");
            Assert.IsTrue(AssemblyScanner.ReferencesNServiceBus(combine, new Dictionary<AssemblyName, AssemblyName>()));
        }

        [Test]
        public void ReferencesNServiceBus_returns_false_for_no_reference()
        {
            var combine = Path.Combine(GetTestAssemblyDirectory(), "dotNet.dll");
            Assert.IsFalse(AssemblyScanner.ReferencesNServiceBus(combine, new Dictionary<AssemblyName, AssemblyName>()));
        }
        [Test]
        public void ReferencesNServiceBus_tracks_already_processed_assemblies()
        {
            var mscorlib = typeof(string).Assembly.GetName();

            var combine = Path.Combine(GetTestAssemblyDirectory(), "AssemblyWithReference.dll");
            var assemblyNames = new Dictionary<AssemblyName, AssemblyName>(AssemblyScanner.Comparer);

            AssemblyScanner.ReferencesNServiceBus(combine, assemblyNames);

            Assert.AreEqual(1, assemblyNames.Count);
            Assert.IsTrue(assemblyNames.ContainsKey(mscorlib));
        }
    }
}