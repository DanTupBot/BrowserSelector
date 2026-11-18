using System;
using System.Reflection;
using Microsoft.Win32;

namespace DanTup.BrowserSelector
{
	static class RegistrySettings
	{
		const string AppID = "DanTup.BrowserSelector";
		const string AppName = "DanTup's Browser Selector";
		const string AppDescription = "DanTup's Browser Selector";
		const string StartMenuInternetKey = "SOFTWARE\\Clients\\StartMenuInternet";
		static readonly string[] Protocols = new[] { "http", "https", "ftp" };
		static string AppPath = Assembly.GetExecutingAssembly().Location;
		static string AppIcon = AppPath + ",0";
		static string AppLaunchCommand = string.Format("\"{0}\"", AppPath);
		static string AppOpenUrlCommand = string.Format("\"{0}\" \"%1\"", AppPath);
		static string AppReinstallCommand = string.Format("\"{0}\" --register", AppPath);
		static string AppHideIconsCommand = string.Format("\"{0}\" --unregister", AppPath);

		static bool IsWindowsXpOrOlder
		{
			get
			{
				return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major < 6;
			}
		}

		internal static void RegisterBrowser()
		{
			string clientKeyPath = string.Format("{0}\\{1}", StartMenuInternetKey, AppID);

			// Register application under Clients\StartMenuInternet (supported on Windows XP through Windows 11).
			using (var appReg = Registry.LocalMachine.CreateSubKey(clientKeyPath))
			{
				appReg.SetValue("", AppName);

				using (var defaultIconReg = appReg.CreateSubKey("DefaultIcon"))
				{
					defaultIconReg.SetValue("", AppIcon);
				}

				using (var openCommandReg = appReg.CreateSubKey("shell\\open\\command"))
				{
					openCommandReg.SetValue("", AppLaunchCommand);
				}

				using (var installInfoReg = appReg.CreateSubKey("InstallInfo"))
				{
					installInfoReg.SetValue("ReinstallCommand", AppReinstallCommand);
					installInfoReg.SetValue("ShowIconsCommand", AppReinstallCommand);
					installInfoReg.SetValue("HideIconsCommand", AppHideIconsCommand);
					installInfoReg.SetValue("IconsVisible", 1, RegistryValueKind.DWord);
				}

				// Register capabilities (used by Default Programs on Windows Vista+).
				using (var capabilityReg = appReg.CreateSubKey("Capabilities"))
				{
					capabilityReg.SetValue("ApplicationName", AppName);
					capabilityReg.SetValue("ApplicationIcon", AppIcon);
					capabilityReg.SetValue("ApplicationDescription", AppDescription);

					using (var startMenuReg = capabilityReg.CreateSubKey("StartMenu"))
					{
						startMenuReg.SetValue("StartMenuInternet", AppID);
					}

					// Set up protocols we want to handle.
					using (var urlAssocReg = capabilityReg.CreateSubKey("URLAssociations"))
					{
						foreach (string protocol in Protocols)
							urlAssocReg.SetValue(protocol, AppID + "URL");
					}
				}
			}

			// Register as application.
			using (var registeredAppsReg = Registry.LocalMachine.CreateSubKey("SOFTWARE\\RegisteredApplications"))
			{
				registeredAppsReg.SetValue(AppID, string.Format("{0}\\Capabilities", clientKeyPath));
			}

			// Set URL Handler.
			using (var handlerReg = Registry.LocalMachine.CreateSubKey(string.Format("SOFTWARE\\Classes\\{0}URL", AppID)))
			{
				handlerReg.SetValue("", AppName);
				handlerReg.SetValue("FriendlyTypeName", AppName);
				handlerReg.SetValue("URL Protocol", "");

				using (var handlerIconReg = handlerReg.CreateSubKey("DefaultIcon"))
				{
					handlerIconReg.SetValue("", AppIcon);
				}

				using (var handlerShellReg = handlerReg.CreateSubKey("shell"))
				{
					handlerShellReg.SetValue("", "open");
					using (var handlerCommandReg = handlerShellReg.CreateSubKey("open\\command"))
					{
						handlerCommandReg.SetValue("", AppOpenUrlCommand);
					}
				}
			}

			// On Windows XP, Default Programs / RegisteredApplications does not exist;
			// set StartMenuInternet and direct protocol handlers for http/https/ftp.
			if (IsWindowsXpOrOlder)
			{
				using (var startMenuRoot = Registry.LocalMachine.CreateSubKey(StartMenuInternetKey))
				{
					startMenuRoot.SetValue("", AppID);
				}

				using (var currentUserStartMenu = Registry.CurrentUser.OpenSubKey("Software\\Clients\\StartMenuInternet", true))
				{
					if (currentUserStartMenu != null)
					{
						currentUserStartMenu.SetValue("", AppID);
					}
				}

				foreach (string protocol in Protocols)
				{
					RegisterXpProtocolHandler(Registry.LocalMachine, "SOFTWARE\\Classes\\" + protocol);

					bool hasUserOverride;
					using (var cuProto = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + protocol))
					{
						hasUserOverride = cuProto != null;
					}

					if (hasUserOverride)
					{
						RegisterXpProtocolHandler(Registry.CurrentUser, "Software\\Classes\\" + protocol);
					}
				}
			}
		}

		internal static void UnregisterBrowser()
		{
			DeleteSubKeyTreeIfExists(Registry.LocalMachine, string.Format("SOFTWARE\\{0}", AppID));
			DeleteSubKeyTreeIfExists(Registry.LocalMachine, string.Format("{0}\\{1}", StartMenuInternetKey, AppID));

			using (var registeredAppsReg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\RegisteredApplications", true))
			{
				if (registeredAppsReg != null)
					registeredAppsReg.DeleteValue(AppID, false);
			}

			DeleteSubKeyTreeIfExists(Registry.LocalMachine, string.Format("SOFTWARE\\Classes\\{0}URL", AppID));

			if (IsWindowsXpOrOlder)
			{
				RestoreStartMenuDefaultIfApp(Registry.LocalMachine, StartMenuInternetKey);
				RestoreStartMenuDefaultIfApp(Registry.CurrentUser, "Software\\Clients\\StartMenuInternet");

				foreach (string protocol in Protocols)
				{
					RestoreXpProtocolHandlerIfApp(Registry.LocalMachine, "SOFTWARE\\Classes\\" + protocol);
					RestoreXpProtocolHandlerIfApp(Registry.CurrentUser, "Software\\Classes\\" + protocol);
				}
			}
		}

		static void RegisterXpProtocolHandler(RegistryKey root, string protocolKeyPath)
		{
			using (var protocolReg = root.CreateSubKey(protocolKeyPath))
			{
				using (var iconReg = protocolReg.CreateSubKey("DefaultIcon"))
				{
					iconReg.SetValue("", AppIcon);
				}

				using (var shellReg = protocolReg.CreateSubKey("shell"))
				{
					shellReg.SetValue("", "open");
					using (var commandReg = shellReg.CreateSubKey("open\\command"))
					{
						commandReg.SetValue("", AppOpenUrlCommand);
					}
				}
			}

			DeleteSubKeyTreeIfExists(root, protocolKeyPath + "\\shell\\open\\ddeexec");
		}

		static void RestoreStartMenuDefaultIfApp(RegistryKey root, string keyPath)
		{
			using (var key = root.OpenSubKey(keyPath, true))
			{
				if (key != null && string.Equals(key.GetValue("") as string, AppID, StringComparison.OrdinalIgnoreCase))
				{
					key.SetValue("", "IEXPLORE.EXE");
				}
			}
		}

		static void RestoreXpProtocolHandlerIfApp(RegistryKey root, string protocolKeyPath)
		{
			using (var commandReg = root.OpenSubKey(protocolKeyPath + "\\shell\\open\\command", true))
			{
				if (commandReg != null)
				{
					string currentCommand = commandReg.GetValue("") as string;
					if (currentCommand != null && currentCommand.IndexOf(AppPath, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						commandReg.SetValue("", "\"C:\\Program Files\\Internet Explorer\\iexplore.exe\" \"%1\"");
					}
				}
			}
		}

		static void DeleteSubKeyTreeIfExists(RegistryKey root, string subKeyPath)
		{
			bool exists;
			using (var key = root.OpenSubKey(subKeyPath))
			{
				exists = key != null;
			}

			if (exists)
			{
				root.DeleteSubKeyTree(subKeyPath);
			}
		}
	}
}
