using System.Linq;
using Topshelf.Internal;
using Topshelf.Internal.ArgumentParsing;

namespace NServiceBus.Host.Internal.Arguments
{
    internal class HostArguments
    {
        public HostArguments(Parser.Args arguments)
        {
            this.Help = GetArgument(arguments, "help") ?? GetArgument(arguments, "?");
            this.ServiceName = GetArgument(arguments, "serviceName");
            this.DisplayName = GetArgument(arguments, "displayName");
            this.Description = GetArgument(arguments, "description");
            this.DependsOn = GetArgument(arguments, "dependsOn");
            this.StartManually = GetArgument(arguments, "startManually");
            this.Username = GetArgument(arguments, "username");
            this.Password = GetArgument(arguments, "password");
        }

        public IArgument Help { get; set; }
        public IArgument ServiceName { get; set; }
        public IArgument DisplayName { get; set; }
        public IArgument Description { get; set; }
        public IArgument DependsOn { get; set; }
        public IArgument StartManually { get; set; }
        public IArgument Username { get; set; }
        public IArgument Password { get; set; }

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