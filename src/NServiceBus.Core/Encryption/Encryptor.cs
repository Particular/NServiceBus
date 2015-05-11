namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using NServiceBus.Encryption;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// Used to configure encryption.
    /// </summary>
    public class Encryptor:Feature
    {
        Func<IBuilder, IEncryptionService> serviceConstructor;

        internal Encryptor()
        {
            EnableByDefault();
            Prerequisite(VerifyPrerequisite, "No encryption properties was found in available messages");
        }

        bool VerifyPrerequisite(FeatureConfigurationContext context)
        {
            var encryptedProperties = GetEncryptedProperties(context);
            var encryptionServiceConstructorDefined = context.Settings.GetEncryptionServiceConstructor(out serviceConstructor);
            var encryptionPropertiesFound = encryptedProperties.Any();
            if (encryptionPropertiesFound)
            {
                if (!encryptionServiceConstructorDefined)
                {
                    var stringBuilder = new StringBuilder("Encrypted properties were found but no encryption service has been defined. Please call ConfigurationBuilder.RijndaelEncryptionService or ConfigurationBuilder.RegisterEncryptionService. Encrypted properties: ");
                    foreach (var encryptedProperty in encryptedProperties)
                    {
                        stringBuilder.AppendFormat("{0}.{1}\r\n", encryptedProperty.DeclaringType, encryptedProperty.Name);
                    }
                    throw new Exception(stringBuilder.ToString());
                }
            }
            else
            {
                if (encryptionServiceConstructorDefined)
                {
                    var message = 
@"You have configured a encryption service via either ConfigurationBuilder.RijndaelEncryptionService or ConfigurationBuilder.RegisterEncryptionService however no properties were found on type that require encryption. 
Perhaps you forgot to define your encryption message conventions or to define message properties using as WireEncryptedString.";
                    log.Warn(message);
                }
            }
            return encryptionPropertiesFound;
        }

        static List<PropertyInfo> GetEncryptedProperties(FeatureConfigurationContext context)
        {
            var conventions = context.Settings.Get<Conventions>();
            return context.Settings.GetAvailableTypes()
                .SelectMany(messageType => messageType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(conventions.IsEncryptedProperty)
                .ToList();
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(serviceConstructor, DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<EncryptionMutator>(DependencyLifecycle.InstancePerCall);

            context.MainPipeline.Register<EncryptBehavior.EncryptRegistration>();
            context.MainPipeline.Register<DecryptBehavior.DecryptRegistration>();
        }
        static ILog log = LogManager.GetLogger<Encryptor>();
    }
}

