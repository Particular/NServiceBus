using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.Config.UnitTests
{
    [TestFixture]
    public class When_selecting_assemblies
    {       
        [Test]
        public void Should_exclude_by_name_without_extension()
        {
            var found = AllAssemblies.Except("rhino.mocks").ToArray();

            Assert.False(
                found.Any(a => a.GetName().Name == "Rhino.Mocks"));
        }

        [Test]
        public void Should_exclude_by_name_without_extension_and_with_upper_letters()
        {
            var found = AllAssemblies.Except("Rhino.Mocks").ToArray();

            Assert.False(
                found.Any(a => a.GetName().Name == "Rhino.Mocks"));
        }

        [Test]
        public void Should_exclude_by_name_with_extension()
        {
            var found = AllAssemblies.Except("rhino.mocks.dll").ToArray();

            Assert.False(
                found.Any(a => a.GetName().Name == "Rhino.Mocks"));
        }

        [Test]
        public void Should_exclude_by_name_with_expression()
        {
            var found = AllAssemblies.Except("rhino.").ToArray();

            Assert.False(
                found.Any(a => a.GetName().Name == "Rhino.Mocks"));
        }

        [Test]
        public void Should_include_rhino_mocks_by_expression()
        {
            var found = AllAssemblies
                .Matching("rhino.").ToArray();

            Assert.True(found.Any(a => a.GetName().Name == "Rhino.Mocks"));
        }

        [Test]
        public void Should_include_NServiceBus_by_default()
        {
            var found = AllAssemblies
                .Matching("foo.bar").ToArray();

            Assert.True(found.Any(a => a.GetName().Name.StartsWith("NServiceBus.")));
        }

        [Test]
        public void Should_exclude_NServiceBus_if_wanted()
        {
            var found = AllAssemblies
                .Except("nservicebus.")
                .ToArray();

            Assert.False(found.Any(a => a.GetName().Name.StartsWith("NServiceBus.")));
        }

        [Test]
        public void Should_exclude_some_NServiceBus_if_wanted()
        {
            var found = AllAssemblies
                .Except("nservicebus")
                .And("nservicebus.configure")
                .ToArray();

            Assert.False(found.Any(a => a.GetName().Name == "NServiceBus"));
            Assert.False(found.Any(a => a.GetName().Name == "NServiceBus.Configure"));
        }

        [Test]
        public void Should_exclude_NServiceBus_after_including_rhino_if_wanted()
        {
            var found = AllAssemblies
                .Matching("rhino.")
                .Except("nservicebus.")
                .ToArray();

            Assert.True(found.Any(a => a.GetName().Name == "Rhino.Mocks"), "Rhino.Mocks not found");
            Assert.False(found.Any(a => a.GetName().Name.StartsWith("NServiceBus.")), "NServiceBus-Assembly found");
        }

        [Test]
        public void Should_include_rhino_using_And()
        {
            var found = AllAssemblies
                .Matching("foo.bar")
                .And("rhino.")
                .ToArray();

            Assert.True(found.Any(a => a.GetName().Name == "Rhino.Mocks"));
        }

        [Test]
        public void Should_use_Appdomain_Assemblies()
        {
            // make sure Rhino.Mocks is loaded
            var repo = new MockRepository();

            var someDir = Path.Combine(Path.GetTempPath(), "empty");

            var found = Configure.FindAssemblies(someDir, true, null, null);

            Assert.True(found.Any(a => a.GetName().Name == "Rhino.Mocks"));
        }
    }
}