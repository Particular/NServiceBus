namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Utils;

    /// <summary>
    /// Used to control the various features supported by the framework.
    /// </summary>
    public abstract class Feature
    {

        /// <summary>
        /// Called when the features is activated
        /// </summary>
        protected abstract void Setup(FeatureConfigurationContext context);

        /// <summary>
        /// Adds a setup prerequisite condition. If false this feature won't be setup
        /// Prerequisites are only evaluated if the feature is enabled
        /// </summary>
        /// <param name="condition">Condition that must be met in order for this feature to be activated</param>
        /// <returns></returns>
        protected void Prerequisite(Func<FeatureConfigurationContext, bool> condition)
        {
            setupPrerequisites.Add(condition);
        }

        /// <summary>
        /// Feature name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Marks this feature as enabled by default. 
        /// </summary>
        protected void EnableByDefault()
        {
            isEnabledByDefault = true;
        }

     
        /// <summary>
        /// The version for this feature
        /// </summary>
        public string Version
        {
            get
            {
                return FileVersionRetriever.GetFileVersion(GetType());
            }
        }

        /// <summary>
        /// Registers this feature as depending on the given feature. This means that this feature won't be activated unless the dependant feature is actived.
        /// This also causes this feature to be activated after the other feature
        /// </summary>
        /// <typeparam name="T">Feature that this feature depends on</typeparam>
        protected void DependsOn<T>() where T:Feature
        {
            dependencies.Add(typeof(T));
        }

        protected void RegisterStartupTask<T>() where T : FeatureStartupTask
        {
            startupTasks.Add(typeof(T));
        }

        /// <summary>
        /// The list of features that this feature is depending on
        /// </summary>
        internal IEnumerable<Type> Dependencies
        {
            get { return dependencies.ToList(); }
        }

        /// <summary>
        /// Tells if this feature is enabled by default
        /// </summary>
        public bool IsEnabledByDefault
        {
            get { return isEnabledByDefault; }
        }


        /// <summary>
        /// Indicates that the feature is active
        /// </summary>
        public bool IsActive { get { return isActive; } }
        

        public override string ToString()
        {
            return string.Format("{0} [{1}]",Name, Version);
        }

        bool Equals(Feature other)
        {
            return other != null && string.Equals(name, other.name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((Feature)obj);
        }

        public override int GetHashCode()
        {
            return (name != null ? name.GetHashCode() : 0);
        }


        public static bool operator ==(Feature feature1, Feature feature2)
        {
            if (ReferenceEquals(feature1, null))
            {
                return ReferenceEquals(feature2, null);
            }

            return feature1.Equals(feature2);
        }

        public static bool operator !=(Feature feature1, Feature feature2)
        {
            return !(feature1 == feature2);
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

        protected Feature()
        {
            name = GetType().Name.Replace("Feature", String.Empty);
        }
        internal IEnumerable<Type> StartupTasks { get { return startupTasks; } } 
        
        List<Type> dependencies = new List<Type>();

        List<Func<FeatureConfigurationContext, bool>> setupPrerequisites = new List<Func<FeatureConfigurationContext, bool>>(); 

        string name;
        bool isEnabledByDefault;

        bool isActive;
        List<Type> startupTasks = new List<Type>();
    }
}