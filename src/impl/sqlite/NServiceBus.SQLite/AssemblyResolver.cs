namespace NServiceBus.SQLite
{
	using System;
	using System.IO;
	using System.Reflection;
	using Config;

	internal class AssemblyResolver : INeedInitialization
	{
		private const string SqliteAssemblyNamespace = "System.Data.SQLite";
		private const string SqliteAssemblyFilename = SqliteAssemblyNamespace + ".dll";
		private const int X86 = 4;

		public void Init()
		{
			AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
			{
				if (!e.Name.Contains(SqliteAssemblyNamespace))
					return null;

				DeleteAssembly();
				WriteAssembly();

				try
				{
					return LoadAssembly();
				}
				catch (BadImageFormatException)
				{
					DeleteAssembly();
					return null;
				}
			};
		}
		private static void WriteAssembly()
		{
			var bytes = IntPtr.Size == X86 ? Binaries.SQLite32 : Binaries.SQLite64;
			using (var writer = File.OpenWrite(SqliteAssemblyFilename))
				writer.Write(bytes, 0, bytes.Length);
		}
		private static void DeleteAssembly()
		{
			File.Delete(SqliteAssemblyFilename);
		}
		private static Assembly LoadAssembly()
		{
			return Assembly.LoadFrom(SqliteAssemblyFilename);
		}
	}
}