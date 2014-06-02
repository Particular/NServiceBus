namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utils;

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
            name = GetType().Name.Replace("Feature", String.Empty);
        }

        /// <summary>
        ///     Feature name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }


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
        internal IEnumerable<Type> DependenciesAll
        {
            get { return dependenciesAll; }
        }

        /// <summary>
        ///     The list of features that this feature is depending on at least one of
        /// </summary>
        internal IEnumerable<Type> DependenciesAny
        {
            get { return dependenciesAny; }
        }

        /// <summary>
        ///     Tells if this feature is enabled by default
        /// </summary>
        public bool IsEnabledByDefault
        {
            get { return isEnabledByDefault; }
        }


        /// <summary>
        ///     Indicates that the feature is active
        /// </summary>
        public bool IsActive
        {
            get { return isActive; }
        }

        internal IEnumerable<Type> StartupTasks
        {
            get { return startupTasks; }
        }

        /// <summary>
        ///     Called when the features is activated
        /// </summary>
        protected abstract void Setup(FeatureConfigurationContext context);

        /// <summary>
        ///     Adds a setup prerequisite condition. If false this feature won't be setup
        ///     Prerequisites are only evaluated if the feature is enabled
        /// </summary>
        /// <param name="condition">Condition that must be met in order for this feature to be activated</param>
        protected void Prerequisite(Func<FeatureConfigurationContext, bool> condition)
        {
            setupPrerequisites.Add(condition);
        }

        /// <summary>
        ///     Marks this feature as enabled by default.
        /// </summary>
        protected void EnableByDefault()
        {
            isEnabledByDefault = true;
        }

        /// <summary>
        ///     Registers this feature as depending on the given feature. This means that this feature won't be activated unless
        ///     the dependant feature is active.
        ///     This also causes this feature to be activated after the other feature.
        /// </summary>
        /// <typeparam name="T">Feature that this feature depends on</typeparam>
        protected void DependsOn<T>() where T : Feature
        {
            dependenciesAll.Add(typeof(T));
        }

        /// <summary>
        /// Register this feature as depending on at least on of the given features. This means that this feature won't be activated
        /// unless at least one of the provided features in the list is active.
        /// This also causes this feature to be activated after the other features.
        /// </summary>
        /// <param name="features">Features list that this feature require at least one of to be activated.</param>
        protected void DependsOnAny(params Type[] features)
        {
            if (dependenciesAny != null)
                throw new NotSupportedException("You cannot call DependsOnAny multiple times");

            dependenciesAny = new List<Type>();
            foreach (var feature in features)
            {
                if (!feature.IsSubclassOf(typeof(Feature)))
                    throw new Exception("A Feature Can only depend on another Feature; " + feature.FullName + " is not a Feature");

                dependenciesAny.Add(feature);
            }
        }

        /// <summary>
        ///     <see cref="FeatureStartupTask" /> that is executed when the <see cref="Feature" /> is started.
        /// </summary>
        /// <typeparam name="T">A <see cref="FeatureStartupTask" />.</typeparam>
        protected void RegisterStartupTask<T>() where T : FeatureStartupTask
        {
            startupTasks.Add(typeof(T));
        }


        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("{0} [{1}]", Name, Version);
        }

        internal bool ShouldBeSetup(FeatureConfigurationContext config)
        {
            return setupPrerequisites.All(condition => condition(config));
        }


        internal void SetupFeature(FeatureConfigurationContext config)
        {
            Setup(config);

            isActive = true;
        }

        List<Type> dependenciesAll = new List<Type>();
        List<Type> dependenciesAny;

        bool isActive;
        bool isEnabledByDefault;
        string name;
        List<Func<FeatureConfigurationContext, bool>> setupPrerequisites = new List<Func<FeatureConfigurationContext, bool>>();
        List<Type> startupTasks = new List<Type>();
    }
}