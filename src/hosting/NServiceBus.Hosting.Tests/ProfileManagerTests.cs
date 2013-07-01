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
        static List<Assembly> allAssemblies = AssemblyPathHelper.GetAllAssemblies();

        [TestFixture]
        public class When_a_profile_is_a_interface
        {
            public interface InterfaceProfile : IProfile
            {
            }

            public class InterfaceProfileHandler : IHandleProfile<InterfaceProfile>
            {
                internal static bool activated;

                public void ProfileActivated()
                {
                    activated = true;
                }
            }

            [Test]
            public void Should_exist_in_active_profiles()
            {
                var profiles = new[]
                               {
                                   typeof(InterfaceProfile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);

                Assert.Contains(typeof(InterfaceProfile), profileManager.activeProfiles);
            }
            [Test]
            public void Should_be_activated()
            {
                var profiles = new[]
                               {
                                   typeof(InterfaceProfile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsTrue(InterfaceProfileHandler.activated);
            }
            [Test]
            public void Should_be_returned_as_an_implementation()
            {
                var profiles = new[]
                               {
                                   typeof(InterfaceProfile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(InterfaceProfileHandler)));
            }
        }
        [TestFixture]
        public class When_a_profile_is_a_class
        {
            public class ClassProfile : IProfile
            {
            }

            public class ClassProfileHandler : IHandleProfile<ClassProfile>
            {
                internal static bool activated;

                public void ProfileActivated()
                {
                    activated = true;
                }
            }

            [Test]
            public void Should_exist_in_active_profiles()
            {
                var profiles = new[]
                               {
                                   typeof(ClassProfile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);

                Assert.Contains(typeof(ClassProfile), profileManager.activeProfiles);
            }
            [Test]
            public void Should_be_activated()
            {
                var profiles = new[]
                               {
                                   typeof(ClassProfile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsTrue(ClassProfileHandler.activated);
            }
            [Test]
            public void Should_be_returned_as_an_implementation()
            {
                var profiles = new[]
                               {
                                   typeof(ClassProfile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(ClassProfileHandler)));
            }
        }

        [TestFixture]
        public class When_profiles_are_inherited_interfaces
        {
            public interface ChildProfile : BaseProfile
            {
            }
            public interface BaseProfile : IProfile
            {
            }

            public class ChildProfileHandler : IHandleProfile<ChildProfile>
            {
                public static bool activated;

                public void ProfileActivated()
                {
                    activated = true;
                }
            }

            public class BaseProfileHandler : IHandleProfile<BaseProfile>
            {
                public static bool activated;

                public void ProfileActivated()
                {
                    activated = true;
                }
            }
            [Test]
            public void All_profiles_should_be_registered_in_active_profiles()
            {
                var profiles = new[]
                               {
                                   typeof(ChildProfile).FullName,
                                   typeof(BaseProfile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);

                Assert.AreEqual(2, profileManager.activeProfiles.Count);
                Assert.Contains(typeof(ChildProfile), profileManager.activeProfiles);
                Assert.Contains(typeof(BaseProfile), profileManager.activeProfiles);
            }

            [Test]
            public void Both_handlers_should_be_activated_for_child_profile()
            {
                var profiles = new[] { typeof(ChildProfile).FullName };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsTrue(BaseProfileHandler.activated);
                Assert.IsTrue(ChildProfileHandler.activated);
            }
            [Test]
            public void Base_handler_should_be_activated_for_base_profile()
            {
                var profiles = new[] { typeof(ChildProfile).FullName };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsTrue(BaseProfileHandler.activated);
                Assert.IsTrue(ChildProfileHandler.activated);
            }
            [Test]
            public void Should_return_all_implementations_for_child_profile()
            {
                var profiles = new[] { typeof(ChildProfile).FullName };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(ChildProfileHandler)));
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(BaseProfileHandler)));
            }
            [Test]
            public void Should_return_only_base_implementations_for_base_profile()
            {
                var profiles = new[] { typeof(BaseProfile).FullName };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.AreEqual(1, implementations.Count);
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(BaseProfileHandler)));
            }

            [Test]
            public void Should_not_return_child_implementations_when_using_a_base_profile()
            {
                var profiles = new[] { typeof(BaseProfile).FullName };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(BaseProfileHandler)));
                Assert.IsFalse(implementations.Any(x => x.GetType() == typeof(ChildProfileHandler)));
            }

            [Test]
            public void Should_not_duplicate_implementations_when_using_both_profiles()
            {
                var profiles = new[]
                               {
                                   typeof (BaseProfile).FullName,
                                   typeof (ChildProfile).FullName
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.AreEqual(2, implementations.Count);
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(BaseProfileHandler)));
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(ChildProfileHandler)));
            }
        }

        [TestFixture]
        public class When_passing_no_arguments
        {

            [Test]
            public void Should_use_default_profile()
            {
                var profileManager = new ProfileManager(allAssemblies, null, new string[] { }, new List<Type> { typeof(Production) });
                
                Assert.AreEqual(1, profileManager.activeProfiles.Count);
                Assert.AreEqual(typeof(Production), profileManager.activeProfiles.First());
            }
        }

        [TestFixture]
        public class When_a_profile_implements_multiple_profiles
        {
            public interface MyProfile : AlsoThisInterface
            {
            }

            public interface AlsoThisInterface : IProfile
            {
            }

            public class Handler : IHandleProfile<MyProfile>
            {
                public static int activatedCount;

                public void ProfileActivated()
                {
                    activatedCount++;
                }
            }
            [Test]
            public void All_profiles_should_be_registered_in_active_profiles()
            {
                var profiles = new[] {typeof (MyProfile).FullName};
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                Assert.Contains(typeof (MyProfile), profileManager.activeProfiles);
                Assert.Contains(typeof (AlsoThisInterface), profileManager.activeProfiles);
            }
            [Test]
            public void Should_not_duplicate_profiles()
            {
                var profiles = new[] {typeof (MyProfile).FullName};
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                Assert.AreEqual(2, profileManager.activeProfiles.Count);
            }
            [Test]
            public void Handlers_should_be_activated_once()
            {
                var profiles = new[] { typeof(MyProfile).FullName };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.AreEqual(1, Handler.activatedCount  );
            }
        }

     

        [TestFixture]
        public class When_multiple_handlers_for_a_profile_exist
        {

            public interface Profile : IProfile
            {
            }

            public class Handler1 : IHandleProfile<Profile>
            {
                public static bool activated;

                public void ProfileActivated()
                {
                    activated = true;
                }
            }

            public class Handler2 : IHandleProfile<Profile>
            {
                public static bool activated;
                public void ProfileActivated()
                {
                    activated = true;
                }
            }

            [Test]
            public void Should_return_all_implementations()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.AreEqual(2, implementations.Count);
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(Handler2)));
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(Handler1)));
            }

            [Test]
            public void Both_handlers_should_be_activated()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsTrue(Handler1.activated);
                Assert.IsTrue(Handler2.activated);
            }
        }
        [TestFixture]
        public class When_abstract_base_handler
        {
            public interface Profile : IProfile
            {
            }

            public class ChildHandler : AbstractHandler
            {
                public new static bool activated;
                public override void ProfileActivated()
                {
                    activated = true;
                }
            }

            public abstract class AbstractHandler : IHandleProfile<Profile>
            {
                public static bool activated;
                public virtual void ProfileActivated()
                {
                    activated = true;
                }
            }
            [Test]
            public void Should_return_concrete_implementation()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.AreEqual(1, implementations.Count);
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(ChildHandler)));
            }
            [Test]
            public void Only_child_should_be_activated()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsTrue(ChildHandler.activated);
                Assert.IsFalse(AbstractHandler.activated);
            }

        }
        [TestFixture]
        public class When_handler_implements_IWantTheEndpointConfig
        {

            public class ConfigureThisEndpoint : IConfigureThisEndpoint
            {
            }
            public interface Profile : IProfile
            {
            }

            public class Handler : IHandleProfile<Profile>, IWantTheEndpointConfig
            {
                internal static IConfigureThisEndpoint config;

                public void ProfileActivated()
                {
                }

                public IConfigureThisEndpoint Config
                {
                    get { return config; }
                    set { config = value; }
                }
            }
            [Test]
            public void ActiveProfiles_should_be_set()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var configureThisEndpoint = new ConfigureThisEndpoint();
                var profileManager = new ProfileManager(allAssemblies, configureThisEndpoint, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.AreEqual(configureThisEndpoint, Handler.config);
            }

        }
        [TestFixture]
        public class When_handler_implements_IWantTheListOfActiveProfiles
        {
            public interface Profile : IProfile
            {
            }

            public class Handler : IHandleProfile<Profile>, IWantTheListOfActiveProfiles
            {
                internal static IEnumerable<Type> activeProfiles;

                public void ProfileActivated()
                {
                }

                public IEnumerable<Type> ActiveProfiles
                {
                    get { return activeProfiles; }
                    set { activeProfiles = value; }
                }
            }

            [Test]
            public void ActiveProfiles_should_be_set()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsNotNull(Handler.activeProfiles);
                Assert.AreEqual(1, Handler.activeProfiles.Count());
            }


        }

        [TestFixture]
        public class When_profiles_are_activated
        {
            public interface Profile : IProfile
            {
            }

            public class Handler : IHandleProfile<Profile>
            {
                internal static bool activatedCalled;

                public void ProfileActivated()
                {
                    activatedCalled = true;
                }

            }
            [Test]
            public void ProfileActivated_should_be_called()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers();
                Assert.IsTrue(Handler.activatedCalled);
            }


        }
        [TestFixture]
        public class When_child_and_base_handlers_implement_a_profile
        {
            public interface Profile : IProfile
            {
            }

            public class ChildHandler : BaseHandler
            {
                public override void ProfileActivated()
                {
                }
            }

            public class BaseHandler : IHandleProfile<Profile>
            {
                public virtual void ProfileActivated()
                {
                }
            }

            [Test]
            public void Should_return_all_implementations()
            {
                var profiles = new[]
                               {
                                   typeof (Profile).FullName,
                               };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                var implementations = profileManager.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.AreEqual(2, implementations.Count);
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(ChildHandler)));
                Assert.IsTrue(implementations.Any(x => x.GetType() == typeof(BaseHandler)));
            }


        }
    }
}
        //test for class profiles