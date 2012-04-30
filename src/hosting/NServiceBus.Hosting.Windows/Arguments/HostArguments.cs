using System.Linq;
using Topshelf.Internal;
using Topshelf.Internal.ArgumentParsing;

namespace NServiceBus.Hosting.Windows.Arguments
{
    internal class HostArguments
    {
        public HostArguments(Parser.Args arguments)
        {
            Help = GetArgument(arguments, "help") ?? GetArgument(arguments, "?");
            ServiceName = GetArgument(arguments, "serviceName");
            DisplayName = GetArgument(arguments, "displayName");
            Description = GetArgument(arguments, "description");
            EndpointConfigurationType = GetArgument(arguments, "endpointConfigurationType");
            DependsOn = GetArgument(arguments, "dependsOn");
            StartManually = GetArgument(arguments, "startManually");
            Username = GetArgument(arguments, "username");
            Password = GetArgument(arguments, "password");
            SideBySide = GetArgument(arguments, "sideBySide");
            EndpointName = GetArgument(arguments, "endpointName");
            InstallInfrastructure = GetArgument(arguments, "installInfrastructure");
            ScannedAssemblies = GetArgument(arguments, "scannedAssemblies");
        }

        public IArgument SideBySide{ get; set; }
        public IArgument Help { get; set; }
        public IArgument ServiceName { get; set; }
        public IArgument DisplayName { get; set; }
        public IArgument Description { get; set; }
        public IArgument EndpointConfigurationType { get; set; }
        public IArgument DependsOn { get; set; }
        public IArgument StartManually { get; set; }
        public IArgument Username { get; set; }
        public IArgument Password { get; set; }
        public IArgument EndpointName { get; set; }
        public IArgument InstallInfrastructure{ get; set; }
        public IArgument ScannedAssemblies { get; set; }


        private static IArgument GetArgument(Parser.Args arguments, string key)
        {
            IArgument argument = arguments.CustomArguments.Where(x => x.Key != null).SingleOrDefault(x => x.Key.ToUpper() == key.ToUpper());

            if (argument != null)
            {
                arguments.CustomArguments = arguments.CustomArguments.Except(new[] {argument});
            }

            return argument;
        }
    }
}