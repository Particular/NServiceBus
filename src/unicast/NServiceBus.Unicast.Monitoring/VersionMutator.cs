namespace NServiceBus.Unicast.Monitoring
{
    using System.Diagnostics;
    using System.Reflection;
    using MessageMutator;
    using NServiceBus.Config;
    using Transport;

    public class VersionMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {

        /// <summary>
        /// Keeps track of related messages to make auditing possible
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="transportMessage"></param>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            transportMessage.Headers[NServiceBus.Headers.NServiceBusVersion] = NServiceBusVersion;
        }

        /// <summary>
        /// The semver version of NServiceBus
        /// </summary>
        public string NServiceBusVersion { get; set; }
     
        /// <summary>
        /// Initializer
        /// </summary>
        public void Init()
        {
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var semverVersion = string.Format("{0}.{1}.{2}", version.FileMajorPart, version.FileMinorPart, version.FileBuildPart);

            Configure.Instance.Configurer.ConfigureComponent<VersionMutator>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.NServiceBusVersion, semverVersion);
        }
    }
}