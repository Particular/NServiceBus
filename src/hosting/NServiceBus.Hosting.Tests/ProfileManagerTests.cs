namespace NServiceBus.Hosting.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Profiles;

    [TestFixture]
    public class ProfileManagerTests
    {
        public interface MyProfile : IProfile, AlsoThisInterface
        {
        }

        public interface AlsoThisInterface : IProfile
        {
        }

        [Test]
        public void ActiveProfileInMyAssembly()
        {
            List<Assembly> allAssemblies = AssemblyPathHelper.GetAllAssemblies();
            var profileManager = new ProfileManager(allAssemblies, null, new[] {typeof (MyProfile).FullName}, null);
            Assert.IsTrue(profileManager.activeProfiles.Any(x => x == typeof (MyProfile)));
            Assert.IsTrue(profileManager.activeProfiles.Any(x => x == typeof (AlsoThisInterface)));
            Assert.AreEqual(2, profileManager.activeProfiles.Count);
        }
    }
}