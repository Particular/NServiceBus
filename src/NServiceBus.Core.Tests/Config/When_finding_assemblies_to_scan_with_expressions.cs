namespace NServiceBus.Core.Tests.Config
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_finding_assemblies_to_scan_with_expressions
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
        public void Should_include_fsharp_by_expression()
        {
            var found = AllAssemblies
                .Matching("TestAssembly.").ToArray();

            Assert.True(found.Any(a => a.GetName().Name == "TestAssembly"));
        }

        [Test]
        public void Should_include_NServiceBus_by_default()
        {
            var found = AllAssemblies
                .Matching("foo.bar").ToArray();

            Assert.True(found.Any(a => a.GetName().Name.StartsWith("NServiceBus.")));
        }

        [Test]
        public void Should_include_fsharp_using_And()
        {
            var found = AllAssemblies
                .Matching("foo.bar")
                .And("TestAssembly.")
                .ToArray();

            Assert.True(found.Any(a => a.GetName().Name == "TestAssembly"));
        }
    }
}