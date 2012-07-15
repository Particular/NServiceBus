using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NServiceBus.Hosting.Configuration;
using NServiceBus.Hosting.Profiles;
using NUnit.Framework;

namespace NServiceBus.Hosting.Tests
{
    [TestFixture]
    public class ProfileManagerTests
    {

        [Test]
        public void ActiveProfileInMyAssembly()
        {
            var allAssemblies = AssemblyPathHelper.GetAllAssemblies();
            var profileManager = new ProfileManager(allAssemblies,null,new[]{typeof(MyProfile).FullName}, null);
            Assert.IsTrue(profileManager.activeProfiles.Any(x => x == typeof(MyProfile)));
            Assert.IsTrue(profileManager.activeProfiles.Any(x => x == typeof(AlsoThisInterface)));
            Assert.AreEqual(2,profileManager.activeProfiles.Count);

        }
        public interface MyProfile : IProfile, AlsoThisInterface
        {
             
        }
        public interface AlsoThisInterface : IProfile
        {
        }
    }
}