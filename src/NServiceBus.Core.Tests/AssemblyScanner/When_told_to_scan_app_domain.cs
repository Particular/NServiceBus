namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_told_to_scan_app_domain
    {
        class SomeHandlerThatEnsuresThatWeKeepReferencingNsbCore : IHandleMessages<string>
        {
            public Task Handle(string message, IMessageHandlerContext context)
            {
                return TaskEx.CompletedTask;
            }
        }

        [Test]
        public void Should_use_AppDomain_Assemblies_if_flagged()
        {
            var someDir = Path.Combine(Path.GetTempPath(), "empty");
            Directory.CreateDirectory(someDir);

            var results = new AssemblyScanner(someDir)
            {
                IncludeAppDomainAssemblies = true,
            }.GetScannableAssemblies();

            var collection = results.Assemblies.Select(a => a.GetName().Name).ToArray();

            CollectionAssert.Contains(collection, "NServiceBus.Core.Tests");
        }
    }
}