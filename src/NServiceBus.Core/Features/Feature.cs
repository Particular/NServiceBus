﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Settings;

    /// <summary>
    ///     Used to control the various features supported by the framework.
    /// </summary>
    public abstract class Feature
    {
        /// <summary>
        ///     Creates an instance of <see cref="Feature" />.
        /// </summary>
        protected Feature()
        {
            StartupTasks = new List<Type>();
            Dependencies = new List<List<string>>();
            Name = GetFeatureName(GetType());
        }

        /// <summary>
        ///     Feature name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     The version for this feature
        /// </summary>
        public string Version
        {
            get { return FileVersionRetriever.GetFileVersion(GetType()); }
        }

        /// <summary>
        ///     The list of features that this feature is depending on
        /// </summary>
        internal List<List<string>> Dependencies { get; private set; }

        /// <summary>
        ///     Tells if this feature is enabled by default
        /// </summary>
        public bool IsEnabledByDefault { get; private set; }

        /// <summary>
        ///     Indicates that the feature is active
        /// </summary>
        public bool IsActive { get; private set; }

        internal List<Type> StartupTasks { get; private set; }

        /// <summary>
        /// Registers default settings
        /// </summary>
        /// <param name="settings">The settings holder</param>
        protected void Defaults(Action<SettingsHolder> settings)
         {
             defaults.Add(settings);
         }

        /// <summary>
        /// Access to the registered defaults
        /// </summary>
        internal List<Action<SettingsHolder>> RegisteredDefaults { get { return defaults; } }

        /// <summary>
        ///     Called when the features is activated
        /// </summary>
        protected internal abstract void Setup(FeatureConfigurationContext context);

        /// <summary>
        ///     Adds a setup prerequisite condition. If false this feature won't be setup.
        ///     Prerequisites are only evaluated if the feature is enabled.
        /// </summary>
        /// <param name="condition">Condition that must be met in order for this feature to be activated.</param>
        /// <param name="description">Explanation of what this prerequisite checks.</param>
        protected void Prerequisite(Func<FeatureConfigurationContext, bool> condition,string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException("Description can't be empty", "description");
            }

            setupPrerequisites.Add(new SetupPrerequisite
            {
                Condition = condition,
                Description = description
            });
        }

        /// <summary>
        ///     Marks this feature as enabled by default.
        /// </summary>
        protected void EnableByDefault()
        {
            IsEnabledByDefault = true;
        }

        /// <summary>
        ///     Registers this feature as depending on the given feature. This means that this feature won't be activated unless
        ///     the dependant feature is active.
        ///     This also causes this feature to be activated after the other feature.
        /// </summary>
        /// <typeparam name="T">Feature that this feature depends on.</typeparam>
        protected void DependsOn<T>() where T : Feature
        {
            DependsOn(GetFeatureName(typeof(T)));
        }

        /// <summary>
        ///     Registers this feature as depending on the given feature. This means that this feature won't be activated unless
        ///     the dependant feature is active.
        ///     This also causes this feature to be activated after the other feature.
        /// </summary>
        /// <param name="featureName">The name of the feature that this feature depends on.</param>
        protected void DependsOn(string featureName)
        {
            Dependencies.Add(new List<string>{featureName});
        }

        /// <summary>
        ///     Register this feature as depending on at least on of the given features. This means that this feature won't be
        ///     activated
        ///     unless at least one of the provided features in the list is active.
        ///     This also causes this feature to be activated after the other features.
        /// </summary>
        /// <param name="features">Features list that this feature require at least one of to be activated.</param>
        protected void DependsOnAtLeastOne(params Type[] features)
        {
            if (features == null)
            {
                throw new ArgumentNullException("features");
            }

            foreach (var feature in features)
            {
                if (!feature.IsSubclassOf(baseFeatureType))
                {
                    throw new ArgumentException(string.Format("A Feature can only depend on another Feature. '{0}' is not a Feature", feature.FullName), "features");
                }
            }

            Dependencies.Add(new List<string>(features.Select(GetFeatureName)));
        }

        /// <summary>
        ///     Register this feature as depending on at least on of the given features. This means that this feature won't be
        ///     activated unless at least one of the provided features in the list is active.
        ///     This also causes this feature to be activated after the other features.
        /// </summary>
        /// <param name="featureNames">The name of the features that this feature depends on.</param>
        protected void DependsOnAtLeastOne(params string[] featureNames)
        {
            if (featureNames == null)
            {
                throw new ArgumentNullException("featureNames");
            }

            Dependencies.Add(new List<string>(featureNames));
        }

        /// <summary>
        ///     <see cref="FeatureStartupTask" /> that is executed when the <see cref="Feature" /> is started.
        /// </summary>
        /// <typeparam name="T">A <see cref="FeatureStartupTask" />.</typeparam>
        protected void RegisterStartupTask<T>() where T : FeatureStartupTask
        {
            StartupTasks.Add(typeof(T));
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("{0} [{1}]", Name, Version);
        }

        internal PrerequisiteStatus CheckPrerequisites(FeatureConfigurationContext context)
        {
            var status = new PrerequisiteStatus();

            foreach (var prerequisite in setupPrerequisites)
            {
                if (!prerequisite.Condition(context))
                {
                    status.ReportFailure(prerequisite.Description);
                }
            }

            return status;
        }

        internal void SetupFeature(FeatureConfigurationContext config)
        {
            Setup(config);

            IsActive = true;
        }

        static string GetFeatureName(Type featureType)
        {
            var name = featureType.Name;

            if (name.EndsWith("Feature"))
            {
                if (name.Length > featureStringLength)
                {
                    name = name.Substring(0, name.Length - featureStringLength);
                }
            }

            return name;
        }

        static Type baseFeatureType = typeof(Feature);
        static int featureStringLength = "Feature".Length;
        List<SetupPrerequisite> setupPrerequisites = new List<SetupPrerequisite>();
        List<Action<SettingsHolder>> defaults = new List<Action<SettingsHolder>>();

        class SetupPrerequisite
        {
            public string Description;
            public Func<FeatureConfigurationContext, bool> Condition;
        }
    }
}