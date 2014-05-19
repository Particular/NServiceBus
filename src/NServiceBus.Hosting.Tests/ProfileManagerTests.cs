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

                public void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
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

                public void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
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

                public void ProfileActivated(Configure config)
                {
                    activated = true;
                }
            }

            public class BaseProfileHandler : IHandleProfile<BaseProfile>
            {
                public static bool activated;

                public void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
                Assert.IsTrue(BaseProfileHandler.activated);
                Assert.IsTrue(ChildProfileHandler.activated);
            }
            [Test]
            public void Base_handler_should_be_activated_for_base_profile()
            {
                var profiles = new[] { typeof(ChildProfile).FullName };
                var profileManager = new ProfileManager(allAssemblies, null, profiles, null);
                profileManager.ActivateProfileHandlers(null);
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

                public void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
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

                public void ProfileActivated(Configure config)
                {
                    activated = true;
                }
            }

            public class Handler2 : IHandleProfile<Profile>
            {
                public static bool activated;
                public void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
                Assert.IsTrue(Handler1.activated);
                Assert.IsTrue(Handler2.activated);
            }
        }

        [TestFixture]
        public class When_multiple_profile_exist
        {
            static List<Type> activations = new List<Type>();

            public interface Profile1 : IProfile
            {
            }

            public interface Profile2 : IProfile
            {
            }

            public class Handler1 : IHandleProfile<Profile1>
            {
                public void ProfileActivated(Configure config)
                {
                    activations.Add(GetType());
                }
            }

            public class Handler2 : IHandleProfile<Profile2>
            {
                public void ProfileActivated(Configure config)
                {
                    activations.Add(GetType());
                }
            }

            [Test]
            public void Should_be_correctly_ordered_in_active_profiles()
            {
                var profilesA = new[]
                               {
                                   typeof (Profile1).FullName,
                                   typeof (Profile2).FullName,
                               };
                var profileManagerA = new ProfileManager(allAssemblies, null, profilesA, null);
                Assert.AreEqual(typeof(Profile1), profileManagerA.activeProfiles[0]);
                Assert.AreEqual(typeof(Profile2), profileManagerA.activeProfiles[1]);

                var profilesB = new[]
                               {
                                   typeof (Profile2).FullName,
                                   typeof (Profile1).FullName,
                               };
                var profileManagerB = new ProfileManager(allAssemblies, null, profilesB, null);
                Assert.AreEqual(typeof(Profile2), profileManagerB.activeProfiles[0]);
                Assert.AreEqual(typeof(Profile1), profileManagerB.activeProfiles[1]);
            }
            
            [Test]
            public void Should_get_implementations_in_order()
            {
                var profilesA = new[]
                               {
                                   typeof (Profile1).FullName,
                                   typeof (Profile2).FullName,
                               };
                var profileManagerA = new ProfileManager(allAssemblies, null, profilesA, null);
                var implementationsA = profileManagerA.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.IsInstanceOf<Handler1>(implementationsA[0]);
                Assert.IsInstanceOf<Handler2>(implementationsA[1]);

                var profilesB = new[]
                               {
                                   typeof (Profile2).FullName,
                                   typeof (Profile1).FullName,
                               };
                var profileManagerB = new ProfileManager(allAssemblies, null, profilesB, null);
                var implementationsB = profileManagerB.GetImplementor<IHandleProfile>(typeof(IHandleProfile<>))
                                                     .ToList();
                Assert.IsInstanceOf<Handler2>(implementationsB[0]);
                Assert.IsInstanceOf<Handler1>(implementationsB[1]);
            }
            [Test]
            public void Should_activate_in_order()
            {
                var profilesA = new[]
                               {
                                   typeof (Profile1).FullName,
                                   typeof (Profile2).FullName,
                               };
                var profileManagerA = new ProfileManager(allAssemblies, null, profilesA, null);
                profileManagerA.ActivateProfileHandlers(null);
                CollectionAssert.AreEqual(new[] { typeof(Handler1), typeof(Handler2) }, activations);

                activations.Clear();
                
                var profilesB = new[]
                               {
                                   typeof (Profile2).FullName,
                                   typeof (Profile1).FullName,
                               };
                var profileManagerB = new ProfileManager(allAssemblies, null, profilesB, null);
                profileManagerB.ActivateProfileHandlers(null);
                CollectionAssert.AreEqual(new[] { typeof(Handler2), typeof(Handler1) }, activations);

            }

        }
        [TestFixture]
        public class When_handler_handles_multiple_profiles
        {
            static List<IHandleProfile> activations = new List<IHandleProfile>();

            public interface Profile1 : IProfile
            {
            }

            public interface Profile2 : IProfile
            {
            }

            public class Handler : IHandleProfile<Profile1>, IHandleProfile<Profile2>
            {
                public void ProfileActivated(Configure config)
                {
                    activations.Add(this);
                }
            }

            [Test]
            public void Should_activate_once_only()
            {
                var profilesA = new[]
                               {
                                   typeof (Profile1).FullName,
                                   typeof (Profile2).FullName,
                               };
                var profileManagerA = new ProfileManager(allAssemblies, null, profilesA, null);
                profileManagerA.ActivateProfileHandlers(null);
                Assert.IsInstanceOf<Handler>(activations[0]);
                Assert.AreEqual(1, activations.Count);
            }
        }
        [TestFixture]
        public class When_handling_profiles_with_inheritance
        {
            static List<Type> activations = new List<Type>();

            public interface BaseProfile : IProfile
            {
            }

            public interface SpecializedProfile : BaseProfile
            {
            }

            public class BaseHandler : IHandleProfile<BaseProfile>
            {
                public void ProfileActivated(Configure config)
                {
                    activations.Add(GetType());
                }
            }

            public class SpecializedHandler : IHandleProfile<SpecializedProfile>
            {
                public void ProfileActivated(Configure config)
                {
                    activations.Add(GetType());
                }
            }

            [Test]
            public void Defining_specialized_profile_should_activate_both_handlers()
            {
                activations.Clear();

                var profilesA = new[]
                               {
                                   typeof (SpecializedProfile).FullName
                               };
                var profileManagerA = new ProfileManager(allAssemblies, null, profilesA, null);
                profileManagerA.ActivateProfileHandlers(null);
                CollectionAssert.AreEquivalent(new[] { typeof(BaseHandler), typeof(SpecializedHandler) }, activations);
            }

            [Test]
            public void Defining_base_profile_should_only_activate_base_handler()
            {
                activations.Clear();

                var profilesA = new[]
                               {
                                   typeof (BaseProfile).FullName
                               };
                var profileManagerA = new ProfileManager(allAssemblies, null, profilesA, null);
                profileManagerA.ActivateProfileHandlers(null);
                CollectionAssert.AreEquivalent(new[]{typeof(BaseHandler)}, activations);
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
                public override void ProfileActivated(Configure config)
                {
                    activated = true;
                }
            }

            public abstract class AbstractHandler : IHandleProfile<Profile>
            {
                public static bool activated;
                public virtual void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
                Assert.IsTrue(ChildHandler.activated);
                Assert.IsFalse(AbstractHandler.activated);
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

                public void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
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

                public void ProfileActivated(Configure config)
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
                profileManager.ActivateProfileHandlers(null);
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
                public override void ProfileActivated(Configure config)
                {
                }
            }

            public class BaseHandler : IHandleProfile<Profile>
            {
                public virtual void ProfileActivated(Configure config)
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