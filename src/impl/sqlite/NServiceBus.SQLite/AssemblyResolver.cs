namespace NServiceBus.SQLite
{
	using System;
	using System.Configuration;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Web;
	using Config;

	public class AssemblyResolver : INeedInitialization
	{
		private const string SqliteAssemblyNamespace = "System.Data.SQLite";
		private const string SqliteAssemblyMask = "*.dll";
		private const string SqliteAssemblyFilename = SqliteAssemblyNamespace + ".dll";
		private const int X86 = 4;

		public void Init()
		{
			if (ContainsAssembly())
				LoadAssembly();

			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				if (!args.Name.Contains(SqliteAssemblyNamespace))
					return null;

				WriteAssembly();
				return LoadAssembly();
			};
		}

		private static bool ContainsAssembly()
		{
			var runtimeDirectory = (HttpContext.Current != null)
				? HttpRuntime.BinDirectory : AppDomain.CurrentDomain.BaseDirectory;

			try
			{
				return Directory.GetFiles(runtimeDirectory, SqliteAssemblyMask).Where(IsSqliteAssembly).Any();
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}
		private static bool IsSqliteAssembly(string filename)
		{
			return (filename ?? string.Empty).EndsWith(
				SqliteAssemblyFilename, StringComparison.InvariantCultureIgnoreCase);
		}

		private static void WriteAssembly()
		{
			var bytes = IntPtr.Size == X86 ? Resources.SQLite32 : Resources.SQLite64;
			using (var writer = File.OpenWrite(SqliteAssemblyFilename))
				writer.Write(bytes, 0, bytes.Length);
		}
		private static Assembly LoadAssembly()
		{
			try
			{
				return Assembly.LoadFrom(SqliteAssemblyFilename);
			}
			catch (BadImageFormatException)
			{
				DeleteAssembly();

				return null;
			}
			catch (FileLoadException e)
			{
				DeleteAssembly();

				if (e.Message.Contains(Resources.MixedModeAssembly))
					throw new ConfigurationErrorsException(Resources.ConfigurationErrorsException);

				throw;
			}
		}
		private static void DeleteAssembly()
		{
			try
			{
				File.Delete(SqliteAssemblyFilename);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}