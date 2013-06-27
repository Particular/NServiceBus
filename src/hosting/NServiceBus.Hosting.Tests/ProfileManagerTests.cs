namespace NServiceBus.Hosting.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using Profiles;

    [TestFixture]
    public class ProfileManagerTests
    {
        List<Assembly> allAssemblies = AssemblyPathHelper.GetAllAssemblies();
        public interface MyProfile : IProfile, AlsoThisInterface
        {
        }

        public interface AlsoThisInterface : IProfile
        {
        }

        [Test]
        public void ActiveProfileInMyAssembly()
        {
            var profiles = new[] { typeof(MyProfile).FullName };
            var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
            Assert.Contains(typeof(MyProfile),profileManager.activeProfiles);
            Assert.Contains(typeof(AlsoThisInterface), profileManager.activeProfiles);
            Assert.AreEqual(2, profileManager.activeProfiles.Count);
        }

        [Test]
        public void Should_return_all_implementations_when_using_a_inherited_profile()
        {
            var profiles = new[] { typeof(ChildProfile).FullName };
            var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
            var configureLogging = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                 .ToList();
            Assert.IsTrue(configureLogging.Any(x => x.GetType() == typeof(ChildProfileHandler)));
            Assert.IsTrue(configureLogging.Any(x => x.GetType() == typeof(BaseProfileHandler)));
        }

        [Test]
        public void Should_not_return_all_implementations_when_using_a_base_profile()
        {
            var profiles = new[] { typeof(BaseProfile).FullName };
            var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
            var configureLogging = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                 .ToList();
            Assert.IsTrue(configureLogging.Any(x => x.GetType() == typeof(BaseProfileHandler)));
            Assert.IsFalse(configureLogging.Any(x => x.GetType() == typeof(ChildProfileHandler)));
        }

        [Test]
        public void Should_not_duplicate_implementations_when_using_multiple_profiles()
        {
            var profiles = new[]
                           {
                               typeof(BaseProfile).FullName,
                               typeof(ChildProfile).FullName
                           };
            var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
            var configureLogging = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                 .ToList();
            Assert.AreEqual(2, configureLogging.Count);
            Assert.IsTrue(configureLogging.Any(x => x.GetType() == typeof(BaseProfileHandler)));
            Assert.IsTrue(configureLogging.Any(x => x.GetType() == typeof(ChildProfileHandler)));
        }


        public interface ChildProfile : BaseProfile
        {
        }
        public interface BaseProfile : IProfile
        {
        }

        public class ChildProfileHandler : IHandleProfile<ChildProfile>
        {
            public void ProfileActivated()
            {
            }
        }

        public class BaseProfileHandler : IHandleProfile<BaseProfile>
        {
            public void ProfileActivated()
            {
            }
        }

        //[Test]
        //public void Should_xxx_when_implementations_overlap()
        //{
        //    var profiles = new[]
        //                   {
        //                       typeof(SimpleProfile).FullName,
        //                   };
        //    var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
        //    var configureLogging = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
        //                                         .ToList();
        //    Assert.AreEqual(2, configureLogging.Count);
        //    Assert.IsTrue(configureLogging.Any(x => x.GetType() == typeof(Handler2)));
        //    Assert.IsTrue(configureLogging.Any(x => x.GetType() == typeof(Handler1)));
        //}

        //public interface SimpleProfile : IProfile
        //{
        //}
        //public class Handler1 : IHandleProfile<SimpleProfile>
        //{
        //    public void ProfileActivated()
        //    {
        //    }
        //}

        //public class Handler2 : IHandleProfile<SimpleProfile>
        //{
        //    public void ProfileActivated()
        //    {
        //    }
        //}
    }
}