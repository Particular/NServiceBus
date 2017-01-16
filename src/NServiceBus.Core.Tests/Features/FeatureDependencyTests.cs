namespace NServiceBus.Core.Tests.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureDependencyTests
    {
        static IEnumerable<FeatureCombinations> FeatureCombinationsForTests
        {
            get
            {
                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature1(), new MyFeature2(), new MyFeature3() },
                    ShouldBeActive = false,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature1 { Enabled = true }, new MyFeature2(), new MyFeature3() },
                    ShouldBeActive = true,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature1(), new MyFeature2 { Enabled = true }, new MyFeature3() },
                    ShouldBeActive = false,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnAtLeastOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature1 { Enabled = true }, new MyFeature2(), new MyFeature3() },
                    ShouldBeActive = true,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnAll_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature1 { Enabled = true }, new MyFeature2(), new MyFeature3() },
                    ShouldBeActive = false,
                };
            }
        }

        [TestCaseSource("FeatureCombinationsForTests")]
        public void Should_only_activate_features_if_dependencies_are_met(FeatureCombinations setup)
        {
            var featureSettings = new FeatureActivator(new SettingsHolder());
            var dependingFeature = setup.DependingFeature;
            featureSettings.Add(dependingFeature);
            Array.ForEach(setup.AvailableFeatures, featureSettings.Add);

            featureSettings.SetupFeatures(null, null, null);

            Assert.AreEqual(setup.ShouldBeActive, dependingFeature.IsActive);
        }

        [Test]
        public void Should_activate_upstream_dependencies_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependsOnOne_Feature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new MyFeature1
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            settings.EnableFeatureByDefault<MyFeature1>();

            featureSettings.SetupFeatures(null, null, null);

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature1>(order.First(), "Upstream dependencies should be activated first");
        }

        [Test]
        public void Should_activate_named_dependency_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependsOnOneByName_Feature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new MyFeature2
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            settings.EnableFeatureByDefault<MyFeature2>();

            featureSettings.SetupFeatures(null, null, null);

            Assert.True(dependingFeature.IsActive);
            Assert.IsInstanceOf<MyFeature2>(order.First(), "Upstream dependencies should be activated first");
        }

        [Test]
        public void Should_not_activate_feature_when_named_dependency_disabled()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependsOnOneByName_Feature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new MyFeature2
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            featureSettings.SetupFeatures(null, null, null);

            Assert.False(dependingFeature.IsActive);
            Assert.IsEmpty(order);
        }

        [Test]
        public void Should_activate_all_upstream_dependencies_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependsOnAtLeastOne_Feature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new MyFeature1
            {
                OnActivation = f => order.Add(f)
            };
            var feature2 = new MyFeature2
            {
                OnActivation = f => order.Add(f)
            };
            var feature3 = new MyFeature3
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);
            featureSettings.Add(feature2);
            featureSettings.Add(feature3);

            settings.EnableFeatureByDefault<MyFeature1>();
            settings.EnableFeatureByDefault<MyFeature2>();
            settings.EnableFeatureByDefault<MyFeature3>();

            featureSettings.SetupFeatures(null, null, null);

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature1>(order[0], "Upstream dependencies should be activated first");
            Assert.IsInstanceOf<MyFeature2>(order[1], "Upstream dependencies should be activated first");
            Assert.IsInstanceOf<MyFeature3>(order[2], "Upstream dependencies should be activated first");
        }

        [Test]
        public void Should_activate_all_upstream_dependencies_when_chain_deep()
        {
            var order = new List<Feature>();


            var level1 = new Level1
            {
                OnActivation = f => order.Add(f)
            };
            var level2 = new Level2
            {
                OnActivation = f => order.Add(f)
            };
            var level3 = new Level3
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            //the orders matter here to expose a bug
            featureSettings.Add(level3);
            featureSettings.Add(level2);
            featureSettings.Add(level1);

            featureSettings.SetupFeatures(null, null, null);


            Assert.True(level1.IsActive, "Level1 wasn't activated");
            Assert.True(level2.IsActive, "Level2 wasn't activated");
            Assert.True(level3.IsActive, "Level3 wasn't activated");

            Assert.IsInstanceOf<Level1>(order[0], "Upstream dependencies should be activated first");
            Assert.IsInstanceOf<Level2>(order[1], "Upstream dependencies should be activated first");
            Assert.IsInstanceOf<Level3>(order[2], "Upstream dependencies should be activated first");
        }

        [Test]
        public void Should_throw_exception_when_dependency_cycle_is_found()
        {
            var order = new List<Feature>();


            var level1 = new CycleLevel1
            {
                OnActivation = f => order.Add(f)
            };
            var level2 = new CycleLevel2
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(level1);
            featureSettings.Add(level2);

            Assert.Throws<ArgumentException>(() => featureSettings.SetupFeatures(null, null, null));
        }

        public class Level1 : TestFeature
        {
            public Level1()
            {
                EnableByDefault();
            }
        }

        public class Level2 : TestFeature
        {
            public Level2()
            {
                EnableByDefault();
                DependsOn<Level1>();
            }
        }

        public class Level3 : TestFeature
        {
            public Level3()
            {
                EnableByDefault();
                DependsOn<Level2>();
            }
        }

        public class CycleLevel1 : TestFeature
        {
            public CycleLevel1()
            {
                EnableByDefault();
                DependsOn<CycleLevel2>();
            }
        }

        public class CycleLevel2 : TestFeature
        {
            public CycleLevel2()
            {
                EnableByDefault();
                DependsOn<CycleLevel1>();
            }
        }

        public class MyFeature1 : TestFeature
        {

        }

        public class MyFeature2 : TestFeature
        {

        }

        public class MyFeature3 : TestFeature
        {

        }

        public class DependsOnOne_Feature : TestFeature
        {
            public DependsOnOne_Feature()
            {
                EnableByDefault();
                DependsOn<MyFeature1>();
            }
        }

        public class DependsOnOneByName_Feature : TestFeature
        {
            public DependsOnOneByName_Feature()
            {
                EnableByDefault();
                DependsOn("NServiceBus.Core.Tests.Features.FeatureDependencyTests+MyFeature2");
            }
        }

        public class DependsOnAll_Feature : TestFeature
        {
            public DependsOnAll_Feature()
            {
                EnableByDefault();
                DependsOn<MyFeature1>();
                DependsOn<MyFeature2>();
                DependsOn<MyFeature3>();
            }
        }

        public class DependsOnAtLeastOne_Feature : TestFeature
        {
            public DependsOnAtLeastOne_Feature()
            {
                EnableByDefault();
                DependsOnAtLeastOne(typeof(MyFeature1), typeof(MyFeature2), typeof(MyFeature3));
            }
        }

        public class FeatureCombinations
        {
            public Feature DependingFeature { get; set; }
            public Feature[] AvailableFeatures { get; set; }
            public bool ShouldBeActive { get; set; }
        }
    }
}
