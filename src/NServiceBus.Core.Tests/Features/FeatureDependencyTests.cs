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
        private IEnumerable<FeatureCombinations> FeatureCombinationsForTests
        {
            get
            {
                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature(), new MyFeature2(), new MyFeature3() },
                    ShouldBeActive = false,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature { Enabled = true }, new MyFeature2(), new MyFeature3() },
                    ShouldBeActive = true,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature(), new MyFeature2 { Enabled = true }, new MyFeature3() },
                    ShouldBeActive = false,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnAtLeastOne_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature { Enabled = true }, new MyFeature2(), new MyFeature3() },
                    ShouldBeActive = true,
                };

                yield return new FeatureCombinations
                {
                    DependingFeature = new DependsOnAll_Feature(),
                    AvailableFeatures = new Feature[] { new MyFeature { Enabled = true }, new MyFeature2(), new MyFeature3() },
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

            featureSettings.SetupFeatures(null);

            Assert.AreEqual(setup.ShouldBeActive, dependingFeature.IsActive);
        }

        [Test]
        public void Should_activate_upstream_deps_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependsOnOne_Feature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new MyFeature
            {
                OnActivation = f => order.Add(f)
            };

            var settings = new SettingsHolder();
            var featureSettings = new FeatureActivator(settings);

            featureSettings.Add(dependingFeature);
            featureSettings.Add(feature);

            settings.EnableFeatureByDefault<MyFeature>();

            featureSettings.SetupFeatures(null);

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature>(order.First(), "Upstream deps should be activated first");
        }

        [Test]
        public void Should_activate_all_upstream_deps_first()
        {
            var order = new List<Feature>();

            var dependingFeature = new DependsOnAtLeastOne_Feature
            {
                OnActivation = f => order.Add(f)
            };
            var feature = new MyFeature
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

            settings.EnableFeatureByDefault<MyFeature>();
            settings.EnableFeatureByDefault<MyFeature2>();
            settings.EnableFeatureByDefault<MyFeature3>();

            featureSettings.SetupFeatures(null);

            Assert.True(dependingFeature.IsActive);

            Assert.IsInstanceOf<MyFeature>(order[0], "Upstream deps should be activated first");
            Assert.IsInstanceOf<MyFeature2>(order[1], "Upstream deps should be activated first");
            Assert.IsInstanceOf<MyFeature3>(order[2], "Upstream deps should be activated first");
        }


        public class MyFeature : TestFeature
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
                DependsOn<MyFeature>();
            }
        }

        public class DependsOnAll_Feature : TestFeature
        {
            public DependsOnAll_Feature()
            {
                EnableByDefault();
                DependsOn<MyFeature>();
                DependsOn<MyFeature2>();
                DependsOn<MyFeature3>();
            }
        }

        public class DependsOnAtLeastOne_Feature : TestFeature
        {
            public DependsOnAtLeastOne_Feature()
            {
                EnableByDefault();
                DependsOnAtLeastOne(typeof(MyFeature), typeof(MyFeature2), typeof(MyFeature3));
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
