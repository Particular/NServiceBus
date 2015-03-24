namespace NServiceBus.Core.Tests.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class FeatureDefaultsTests
    {
        public class FeatureThatEnablesAnother : Feature
        {
            public FeatureThatEnablesAnother()
            {
                EnableByDefault();
                Defaults(s => s.EnableFeatureByDefault<FeatureThatIsEnabledByAnother>());
            }

            protected internal override void Setup(FeatureConfigurationContext context)
            {
            }
        }

        public class FeatureThatIsEnabledByAnother : Feature
        {
            public FeatureThatIsEnabledByAnother()
            {
                Defaults(s => DefaultCalled = true);
            }

            public bool DefaultCalled;
            
            protected internal override void Setup(FeatureConfigurationContext context)
            {
            }
        }

        [Test]
        public void Feature_enabled_by_later_feature_should_have_default_called()
        {
            var featureThatIsEnabledByAnother = new FeatureThatIsEnabledByAnother();
            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);
            //the orders matter here to expose a bug
            featureSettings.Add(featureThatIsEnabledByAnother);
            featureSettings.Add(new FeatureThatEnablesAnother());

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.True(featureThatIsEnabledByAnother.DefaultCalled, "FeatureThatIsEnabledByAnother wasn't activated");
        }

        [Test]
        public void Should_enable_features_in_defaults()
        {
            var defaultsOrder = new List<Feature>();
            var activatedOrder = new List<Feature>();

            var level1 = new Activate1
            {
                OnActivation = f => activatedOrder.Add(f),
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var level2 = new Activate2
            {
                OnActivation = f => activatedOrder.Add(f),
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var level3 = new Activate3
            {
                OnActivation = f => activatedOrder.Add(f),
                OnDefaults = f => defaultsOrder.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            //the orders matter here to expose a bug
            featureSettings.Add(level3);
            featureSettings.Add(level2);
            featureSettings.Add(level1);

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.True(level1.IsActive, "Activate1 wasn't activated");
            Assert.True(level2.IsActive, "Activate2 wasn't activated");
            Assert.True(level3.IsActive, "Activate3 wasn't activated");

            Assert.IsInstanceOf<Activate1>(defaultsOrder[0], "Upstream deps should be activated first");
            Assert.IsInstanceOf<Activate2>(defaultsOrder[1], "Upstream deps should be activated first");
            Assert.IsInstanceOf<Activate3>(defaultsOrder[2], "Upstream deps should be activated first");

            CollectionAssert.AreEqual(defaultsOrder, activatedOrder);
        }

        [Test]
        public void Should_activate_upstream_deps_first()
        {
            var defaultsOrder = new List<Feature>();

            var dependingFeature = new DependsOnOne_Feature
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var feature = new MyFeature1
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            settings.EnableFeatureByDefault<MyFeature1>();

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature1>(defaultsOrder.First(), "Upstream deps should be activated first");
        }

        [Test]
        public void Should_activate_all_upstream_deps_first()
        {
            var defaultsOrder = new List<Feature>();

            var dependingFeature = new DependsOnAtLeastOne_Feature
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var feature = new MyFeature1
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var feature2 = new MyFeature2
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var feature3 = new MyFeature3
            {
                OnDefaults = f => defaultsOrder.Add(f)
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

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature1>(defaultsOrder[0], "Upstream deps should be activated first");
            Assert.IsInstanceOf<MyFeature2>(defaultsOrder[1], "Upstream deps should be activated first");
            Assert.IsInstanceOf<MyFeature3>(defaultsOrder[2], "Upstream deps should be activated first");
        }

        [Test]
        public void Should_activate_all_upstream_deps_when_chain_deep()
        {
            var defaultsOrder = new List<Feature>();
           
            var level1 = new Level1
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var level2 = new Level2
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };
            var level3 = new Level3
            {
                OnDefaults = f => defaultsOrder.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            //the orders matter here to expose a bug
            featureSettings.Add(level3);
            featureSettings.Add(level2);
            featureSettings.Add(level1);

            featureSettings.SetupFeatures(new FeatureConfigurationContext(null));

            Assert.True(level1.IsActive, "Level1 wasn't activated");
            Assert.True(level2.IsActive, "Level2 wasn't activated");
            Assert.True(level3.IsActive, "Level3 wasn't activated");

            Assert.IsInstanceOf<Level1>(defaultsOrder[0], "Upstream deps should be activated first");
            Assert.IsInstanceOf<Level2>(defaultsOrder[1], "Upstream deps should be activated first");
            Assert.IsInstanceOf<Level3>(defaultsOrder[2], "Upstream deps should be activated first");
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

        public class Activate1 : TestFeature
        {
            public Activate1()
            {
                EnableByDefault();
                Defaults(s=>s.EnableFeatureByDefault<Activate2>());
            }
        }

        public class Activate2 : TestFeature
        {
            public Activate2()
            {
                DependsOn<Activate1>();
                Defaults(s => s.EnableFeatureByDefault<Activate3>());
            }
        }

        public class Activate3 : TestFeature
        {
            public Activate3()
            {
                DependsOn<Activate2>();
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
    }
}
