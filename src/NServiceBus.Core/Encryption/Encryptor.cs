namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Logging;
    using Unicast.Messages;

    /// <summary>
    /// Used to configure encryption.
    /// </summary>
    public class Encryptor : Feature
    {
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
                    var stringBuilder = new StringBuilder("Encrypted properties were found but no encryption service has been defined. Call endpointConfiguration.RijndaelEncryptionService or endpointConfiguration.RegisterEncryptionService. Encrypted properties: ");
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
                        @"Encryption service has been configured via either endpointConfiguration.RijndaelEncryptionService or endpointConfiguration.RegisterEncryptionService however no properties were found on type that require encryption.
Ensure that either encryption message conventions are defined or to define message properties using as WireEncryptedString.";
                    log.Warn(message);
                }
            }
            return encryptionPropertiesFound;
        }

        static List<PropertyInfo> GetEncryptedProperties(FeatureConfigurationContext context)
        {
            var conventions = context.Settings.Get<Conventions>();
            var registry = context.Settings.Get<MessageMetadataRegistry>();
            return registry.GetAllMessages()
                .SelectMany(metadata => metadata.MessageType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                .Where(conventions.IsEncryptedProperty)
                .ToList();
        }

        /// <summary>
        /// <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var service = serviceConstructor();
            var inspector = new EncryptionInspector(context.Settings.Get<Conventions>());

            context.Pipeline.Register(new EncryptBehavior.EncryptRegistration(inspector, service));
            context.Pipeline.Register(new DecryptBehavior.DecryptRegistration(inspector, service));
        }

        Func<IEncryptionService> serviceConstructor;
        static ILog log = LogManager.GetLogger<Encryptor>();
    }
}