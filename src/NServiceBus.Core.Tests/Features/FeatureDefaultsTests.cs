namespace NServiceBus.Core.Tests.Features;

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

    FeatureActivator featureSettings;
    SettingsHolder settings;

    [SetUp]
    public void Init()
    {
        settings = new SettingsHolder();
        featureSettings = new FeatureActivator(settings);
    }

    [Test]
    public void Feature_enabled_by_later_feature_should_have_default_called()
    {
        var featureThatIsEnabledByAnother = new FeatureThatIsEnabledByAnother();
        //the orders matter here to expose a bug
        featureSettings.Add(featureThatIsEnabledByAnother);
        featureSettings.Add(new FeatureThatEnablesAnother());

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        Assert.That(featureThatIsEnabledByAnother.DefaultCalled, Is.True, "FeatureThatIsEnabledByAnother wasn't activated");
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

        //the orders matter here to expose a bug
        featureSettings.Add(level3);
        featureSettings.Add(level2);
        featureSettings.Add(level1);

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        Assert.Multiple(() =>
        {
            Assert.That(level1.IsActive, Is.True, "Activate1 wasn't activated");
            Assert.That(level2.IsActive, Is.True, "Activate2 wasn't activated");
            Assert.That(level3.IsActive, Is.True, "Activate3 wasn't activated");

            Assert.That(defaultsOrder[0], Is.InstanceOf<Activate1>(), "Upstream dependencies should be activated first");
            Assert.That(defaultsOrder[1], Is.InstanceOf<Activate2>(), "Upstream dependencies should be activated first");
            Assert.That(defaultsOrder[2], Is.InstanceOf<Activate3>(), "Upstream dependencies should be activated first");
        });

        CollectionAssert.AreEqual(defaultsOrder, activatedOrder);
    }

    [Test]
    public void Should_activate_upstream_dependencies_first()
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

        featureSettings.Add(dependingFeature);
        featureSettings.Add(feature);

        settings.EnableFeatureByDefault<MyFeature1>();

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        Assert.Multiple(() =>
        {
            Assert.That(dependingFeature.IsActive, Is.True);

            Assert.That(defaultsOrder.First(), Is.InstanceOf<MyFeature1>(), "Upstream dependencies should be activated first");
        });
    }

    [Test]
    public void Should_activate_all_upstream_dependencies_first()
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

        featureSettings.Add(dependingFeature);
        featureSettings.Add(feature);
        featureSettings.Add(feature2);
        featureSettings.Add(feature3);

        settings.EnableFeatureByDefault<MyFeature1>();
        settings.EnableFeatureByDefault<MyFeature2>();
        settings.EnableFeatureByDefault<MyFeature3>();

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        Assert.Multiple(() =>
        {
            Assert.That(dependingFeature.IsActive, Is.True);

            Assert.That(defaultsOrder[0], Is.InstanceOf<MyFeature1>(), "Upstream dependencies should be activated first");
            Assert.That(defaultsOrder[1], Is.InstanceOf<MyFeature2>(), "Upstream dependencies should be activated first");
            Assert.That(defaultsOrder[2], Is.InstanceOf<MyFeature3>(), "Upstream dependencies should be activated first");
        });
    }

    [Test]
    public void Should_activate_all_upstream_dependencies_when_chain_deep()
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

        //the orders matter here to expose a bug
        featureSettings.Add(level3);
        featureSettings.Add(level2);
        featureSettings.Add(level1);

        featureSettings.SetupFeatures(new FakeFeatureConfigurationContext());

        Assert.Multiple(() =>
        {
            Assert.That(level1.IsActive, Is.True, "Level1 wasn't activated");
            Assert.That(level2.IsActive, Is.True, "Level2 wasn't activated");
            Assert.That(level3.IsActive, Is.True, "Level3 wasn't activated");

            Assert.That(defaultsOrder[0], Is.InstanceOf<Level1>(), "Upstream dependencies should be activated first");
            Assert.That(defaultsOrder[1], Is.InstanceOf<Level2>(), "Upstream dependencies should be activated first");
            Assert.That(defaultsOrder[2], Is.InstanceOf<Level3>(), "Upstream dependencies should be activated first");
        });
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
            Defaults(s => s.EnableFeatureByDefault<Activate2>());
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
