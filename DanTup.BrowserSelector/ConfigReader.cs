using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace DanTup.BrowserSelector
{
	static class ConfigReader
	{
		/// <summary>
		/// Config lives in the same folder as the EXE, name "BrowserSelector.ini".
		/// </summary>
		public static string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BrowserSelector.ini");

		static internal IEnumerable<UrlPreference> GetUrlPreferences()
		{
			if (!File.Exists(ConfigPath))
				throw new InvalidOperationException(string.Format("The config file was not found:\r\n{0}\r\n", ConfigPath));

			// Poor mans INI file reading... Skip comment lines (TODO: support comments on other lines).
			var configLines = new List<string>();
			foreach (string rawLine in File.ReadAllLines(ConfigPath))
			{
				string line = rawLine.Trim();
				if (!string.IsNullOrEmpty(line) && !line.StartsWith(";") && !line.StartsWith("#"))
					configLines.Add(line);
			}

			// Read the browsers section into a dictionary.
			var browsers = new Dictionary<string, Browser>();
			Browser defaultBrowser = null;
			foreach (string line in GetConfig(configLines, "browsers"))
			{
				var kvp = SplitConfig(line);
				var browser = new Browser { Name = kvp.Key, Location = kvp.Value };
				browsers.Add(browser.Name, browser);
				if (defaultBrowser == null)
					defaultBrowser = browser;
			}

			// If there weren't any at all, force IE in there (nobody should create a config file like this!).
			if (browsers.Count == 0)
			{
				defaultBrowser = new Browser { Name = "ie", Location = @"iexplore.exe ""{0}""" };
				browsers.Add("ie", defaultBrowser);
			}

			// Read the url preferences, and add a catchall ("*") for the first browser.
			var urls = new List<UrlPreference>();
			foreach (string line in GetConfig(configLines, "urls"))
			{
				var kvp = SplitConfig(line);
				Browser browser;
				if (browsers.TryGetValue(kvp.Value, out browser))
					urls.Add(new UrlPreference { UrlPattern = kvp.Key, Browser = browser });
			}

			if (defaultBrowser != null)
				urls.Add(new UrlPreference { UrlPattern = "*", Browser = defaultBrowser }); // Add in a catchall that uses the first browser

			return urls;
		}

		static IEnumerable<string> GetConfig(IEnumerable<string> configLines, string configName)
		{
			// Read everything from [configName] up to the next [section].
			string sectionHeader = string.Format("[{0}]", configName);
			bool inSection = false;

			foreach (string line in configLines)
			{
				if (!inSection)
				{
					if (line.StartsWith(sectionHeader, StringComparison.OrdinalIgnoreCase))
						inSection = true;
					continue;
				}

				if (line.StartsWith("[", StringComparison.OrdinalIgnoreCase))
					yield break;

				if (line.IndexOf('=') >= 0)
					yield return line;
			}
		}

		/// <summary>
		/// Splits a line on the first '=' (poor INI parsing).
		/// </summary>
		static KeyValuePair<string, string> SplitConfig(string configLine)
		{
			var parts = configLine.Split(new[] { '=' }, 2);
			return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
		}

		public static void CreateSampleIni()
		{
			Assembly assembly;
			Stream stream;
			StringBuilder result;

			assembly = Assembly.GetExecutingAssembly();
			stream = assembly.GetManifestResourceStream("DanTup.BrowserSelector.BrowserSelector.ini");
			if (stream == null)
			{
				string[] resourceNames = assembly.GetManifestResourceNames();
				if (resourceNames.Length > 0)
					stream = assembly.GetManifestResourceStream(resourceNames[0]);
			}
			if (stream == null)
			{
				return;
			}

			result = new StringBuilder();

			using (StreamReader reader = new StreamReader(stream))
			{
				result.Append(reader.ReadToEnd());
				reader.Close();
			}

			if (result.Length > 0)
			{
				if (File.Exists(ConfigPath))
				{
					string newName = GetBackupFileName(ConfigPath);
					File.Move(ConfigPath, newName);
				}

				File.WriteAllText(ConfigPath, result.ToString());
			}
		}

		static string GetBackupFileName(string fileName)
		{
			string newName;
			string fname;
			string fext;
			int index = 0;

			fname = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
			fext = Path.GetExtension(fileName);

			do
			{
				newName = string.Format("{0}.{1:0000}{2}", fname, ++index, fext);
			} while (File.Exists(newName));

			return newName;
		}
	}

	class Browser
	{
		public string Name { get; set; }
		public string Location { get; set; }
	}

	class UrlPreference
	{
		public string UrlPattern { get; set; }
		public Browser Browser { get; set; }
	}
}
