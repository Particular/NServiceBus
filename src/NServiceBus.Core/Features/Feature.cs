namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
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

        public void SetupFeature(FeatureConfigurationContext config)
        {
            Setup(config);

            isActivated = true;
        }

      
        /// <summary>
        /// Adds a setup prerequisite condition. If false this feature won't be setup
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        protected void Prerequisite(Func<FeatureConfigurationContext, bool> condition)
        {
            setupPrerequisites.Add(condition);
        }


        public  bool ShouldBeSetup(FeatureConfigurationContext config)
        {
            return setupPrerequisites.All(condition => condition(config));
        }

        /// <summary>
        /// Return <c>true</c> if this is a default <see cref="Feature"/> that needs to be turned on automatically.
        /// </summary>
        public bool IsEnabledByDefault
        {
            get { return isEnabledByDefault; }
        }

        /// <summary>
        /// Feature name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        protected void EnableByDefault()
        {
            isEnabledByDefault = true;
        }

     
        public string Version
        {
            get
            {
                return FileVersionRetriever.GetFileVersion(GetType());
            }
        }

        protected void DependsOn<T>() where T:Feature
        {
            dependencies.Add(typeof(T));
        }

        public IEnumerable<Type> Dependencies
        {
            get { return dependencies.ToList(); }
        }

        public bool IsActivated { get { return isActivated; } }
        

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

        protected Feature()
        {
            name = GetType().Name.Replace("Feature", String.Empty);
        }

        List<Type> dependencies = new List<Type>();

        List<Func<FeatureConfigurationContext, bool>> setupPrerequisites = new List<Func<FeatureConfigurationContext, bool>>(); 

        string name;
        bool isEnabledByDefault;

        bool isActivated;
    }

    public class FeatureConfigurationContext
    {
        readonly Configure config;

        public FeatureConfigurationContext(Configure config)
        {
            this.config = config;
            
        }

        public ReadOnlySettings Settings { get { return config.Settings; } }
        public IConfigureComponents Container { get { return config.Configurer; } }
        public PipelineSettings Pipeline { get { return config.Pipeline; } }
        public IEnumerable<Type> TypesToScan { get { return config.TypesToScan; } }
    }
}