using System;
using System.IO;
using System.Text;
using System.Reflection;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Assembly a = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, args[0]));

            if (args.Length == 2)
                baseNameSpace = args[1];

            Events.GuidDetected += delegate
                                       {
                                           needToGenerateGuid = true;
                                       };

            foreach(Type t in a.GetTypes())
                TopLevelScan(t);

            string xsd = GenerateXsdString();

            using(StreamWriter writer = File.CreateText(GetFileName()))
                writer.Write(xsd);

            if (needToGenerateGuid)
                using (StreamWriter writer = File.CreateText(GetFileName()))
                    writer.Write(Strings.GuidXsd);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Usage: first parameter [required], your assembly. second parameter [optional] the base namespace (or http://tempuri.net will be used).");
        }

        private static string GetFileName()
        {
            int i = 0;
            while (File.Exists(string.Format("schema{0}.xsd", i)))
                i++;

            return string.Format("schema{0}.xsd", i);
        }

        private static string GenerateXsdString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.AppendLine("<xs:schema elementFormDefault=\"qualified\" targetNamespace=\"" + baseNameSpace + "/" + nameSpace + "\" xmlns=\"" + baseNameSpace + "/" + nameSpace + "\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">");

            if (needToGenerateGuid)
                builder.AppendLine("<xs:import namespace=\"http://microsoft.com/wsdl/types/\" />");

            foreach (ComplexType complex in Repository.ComplexTypes)
            {
                builder.AppendFormat("<xs:element name=\"{0}\" nillable=\"true\" type=\"{0}\">\n", complex.Name);

                if (complex.TimeToBeReceived > TimeSpan.Zero)
                {
                    builder.AppendLine("<xs:annotation>");
                    builder.AppendLine("<xs:appinfo>");
                    builder.AppendFormat("<TimeToBeReceived>{0}</TimeToBeReceived>\n", complex.TimeToBeReceived);
                    builder.AppendLine("</xs:appinfo>");
                    builder.AppendLine("</xs:annotation>");
                }

                builder.AppendLine("</xs:element>");
                ComplexTypeWriter.Write(complex, builder);
            }

            foreach (Type simple in Repository.SimpleTypes)
            {
                builder.AppendFormat("<xs:element name=\"{0}\" type=\"{0}\" />\n", simple.Name);
                SimpleTypeWriter.Write(simple, builder);
            }

            SimpleTypeWriter.WriteChar(builder);

            builder.AppendLine("</xs:schema>");

            string result = builder.ToString();

            return result;
        }

        public static void TopLevelScan(Type type)
        {
            if (typeof(IMessage).IsAssignableFrom(type))
            {
                if (nameSpace == null)
                    nameSpace = type.Namespace;
                else
                    if (type.Namespace != nameSpace)
                    {
                        Console.WriteLine("WARNING: Not all types are in the same namespace. This may cause serialization to fail and is not supported.");
                        Console.ReadLine();

                        throw new InvalidOperationException("Not all types are in the same namespace");
                    }                 

                Scan(type);
            }
        }

        public static void Scan(Type type)
        {
            if (type == null || type == typeof(object) || type == typeof(IMessage))
                return;

            Repository.Handle(type);

            if (!type.IsInterface)
                Scan(type.BaseType);
            else
                foreach (Type i in type.GetInterfaces())
                    Scan(i);
        }

        private static bool needToGenerateGuid;
        private static string nameSpace;
        private static string baseNameSpace = "http://tempuri.net";
    }
}
