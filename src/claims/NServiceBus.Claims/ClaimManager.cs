using System;
using System.Collections.Generic;
using System.IO;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using NServiceBus.Config;
using NServiceBus.MessageMutator;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Claims
{
    /// <summary>
    /// Manages all aspects of flowing claims between nodes
    /// </summary>
    public class ClaimManager : INeedInitialization, IMutateOutgoingTransportMessages
    {
        void INeedInitialization.Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ClaimManager>(DependencyLifecycle.SingleInstance);

            Configure.ConfigurationComplete += () => {
                    Configure.Instance.Builder.Build<ITransport>().TransportMessageReceived += TransportTransportMessageReceived;
                };
        }

        static void TransportTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            if (!ConfigureClaimFlow.Flow) return;
            if (!e.Message.Headers.ContainsKey(SecurityTokenKey)) return;

            var serializedToken = e.Message.Headers[SecurityTokenKey];
            var certificate = ExtractCertificate(serializedToken);
            var handler = new Saml2SecurityTokenHandler(new SamlSecurityTokenRequirement());
            var tokens = new List<SecurityToken> {new X509SecurityToken(certificate)};
            var resolver = SecurityTokenResolver.CreateDefaultSecurityTokenResolver(tokens.AsReadOnly(), false);
            handler.Configuration = new SecurityTokenHandlerConfiguration
            {
                IssuerTokenResolver = resolver,
                IssuerNameRegistry = new InternalIssuerNameRegistry(),
                CertificateValidator = X509CertificateValidator.None
            };
            using (var reader = XmlReader.Create(new StringReader(serializedToken)))
            {
                var bootstrapToken = handler.ReadToken(reader);
                handler.Configuration.AudienceRestriction.AudienceMode = AudienceUriMode.Never;
                handler.Configuration.MaxClockSkew = TimeSpan.MaxValue;
                var collection = handler.ValidateToken(bootstrapToken);
                Thread.CurrentPrincipal = new ClaimsPrincipal(collection);
            }
           
        }

        private static X509Certificate2 ExtractCertificate(string serializedToken)
        {
            var doc = new XmlDocument();
            doc.LoadXml(serializedToken);

            var xmlnsManager = new XmlNamespaceManager(doc.NameTable);
            xmlnsManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            xmlnsManager.AddNamespace("n", "urn:oasis:names:tc:SAML:2.0:assertion");

            var node = doc.SelectSingleNode("/n:Assertion/ds:Signature/ds:KeyInfo/ds:X509Data/ds:X509Certificate/text()", xmlnsManager);
            // not sure how I should handle absence of certificate yet, blow up? or try another cert?
            var certificatecontent = Encoding.UTF8.GetBytes(node.Value);
            
            return new X509Certificate2(certificatecontent);
        }

        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            if (transportMessage.Headers.ContainsKey(SecurityTokenKey))
                transportMessage.Headers.Remove(SecurityTokenKey);

            var claimsPrincipal = Thread.CurrentPrincipal as IClaimsPrincipal;
            if (claimsPrincipal == null) return;

            var bootstrapToken = claimsPrincipal.Identities[0].BootstrapToken;
            if (bootstrapToken == null) return;

            var handler = new Saml2SecurityTokenHandler(new SamlSecurityTokenRequirement());
            var stringBuilder = new StringBuilder();
            using (var writer = XmlWriter.Create(stringBuilder))
            {
                handler.WriteToken(writer, bootstrapToken);      
            }
            var serializedToken = stringBuilder.ToString();

            transportMessage.Headers.Add(SecurityTokenKey, serializedToken);
        }

        private const string SecurityTokenKey = "SecurityToken";

        internal class InternalIssuerNameRegistry : IssuerNameRegistry
        {
            public override string GetIssuerName(SecurityToken securityToken)
            {
                return "DoNotcareAboutTheIssuer";
            }
        }

    }
}
