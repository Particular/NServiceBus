namespace NServiceBus.Settings
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class SettingsHolderTests
    {
        [Test]
        public void Clear_ShouldDisposeAllDisposables()
        {
            var firstOverrideDisposable = new SomeDisposable();
            var secondOverrideDisposable = new SomeDisposable();
            var firstDefaultDisposable = new SomeDisposable();
            var secondDefaultDisposable = new SomeDisposable();

            var all = new[]
            {
                firstDefaultDisposable,
                secondDefaultDisposable,
                firstOverrideDisposable,
                secondOverrideDisposable
            };

            var settings = new SettingsHolder();
            settings.Set("1.Override", firstOverrideDisposable);
            settings.Set("2.Override", secondOverrideDisposable);
            settings.SetDefault("1.Default", firstDefaultDisposable);
            settings.SetDefault("2.Default", secondDefaultDisposable);

            settings.Clear();

            Assert.IsTrue(all.All(x => x.Disposed));
        }

        [Test]
        public void GetDefaultTest()
        {
            var settings = new SettingsHolder();
            settings.Set("MySetting", "explicitValue");
            settings.SetDefault("MySetting", "defaultValue");

            Assert.AreEqual("defaultValue", settings.GetDefault<string>("MySetting"));
        }

        [Test]
        public void GetConditional_ConditionPassedTest()
        {
            var settings = new SettingsHolder();
            settings.Set("MySetting", "explicitValue");
            settings.SetDefault("MySetting", "defaultValue");

            Assert.AreEqual("explicitValue", settings.GetConditional<string>("MySetting", () => true));
        }

        [Test]
        public void GetConditional_ConditionFailedTest()
        {
            var settings = new SettingsHolder();
            settings.Set("MySetting", "explicitValue");
            settings.SetDefault("MySetting", "defaultValue");

            Assert.AreEqual("defaultValue", settings.GetConditional<string>("MySetting", () => false));
        }

        class SomeDisposable : IDisposable
        {
            public bool Disposed;

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}